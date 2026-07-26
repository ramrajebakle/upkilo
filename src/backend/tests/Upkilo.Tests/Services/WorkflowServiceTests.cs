using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Logging;
using Moq;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Interfaces.Workflow;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;
using WorkflowEntity = Upkilo.Core.Entities.Workflow;

namespace Upkilo.Tests.Services
{
    public class WorkflowServiceTests : IDisposable
    {
        private readonly TestDbContextFactory _dbFactory;
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<ISmsService> _smsServiceMock = new();
        private readonly Mock<IWhatsAppService> _whatsAppServiceMock = new();
        private readonly Mock<IWebhookService> _webhookServiceMock = new();
        private readonly Mock<IBackgroundJobClient> _backgroundJobsMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly Mock<ILogger<WorkflowService>> _loggerMock = new();

        public WorkflowServiceTests()
        {
            _dbFactory = new TestDbContextFactory();
        }

        public void Dispose() => _dbFactory.Dispose();

        private (WorkflowService sut, Upkilo.Infrastructure.Data.AppDbContext ctx, Guid tenantId) CreateSut()
        {
            var ctx = _dbFactory.CreateContext();
            var tenantId = Guid.NewGuid();

            // Seed tenant
            ctx.Tenants.Add(new Tenant
            {
                Id = tenantId,
                Name = "Test Tenant",
                Slug = "test",
                SubscriptionTier = SubscriptionTier.Free
            });
            ctx.SaveChanges();

            var sut = new WorkflowService(
                ctx,
                _emailServiceMock.Object,
                _smsServiceMock.Object,
                _whatsAppServiceMock.Object,
                _webhookServiceMock.Object,
                _backgroundJobsMock.Object,
                _loggerMock.Object,
                _notificationServiceMock.Object,
                new List<IWorkflowStepExecutor>()
            );

            return (sut, ctx, tenantId);
        }

        [Fact]
        public async Task ExecuteWorkflowAsync_StartsExecution_RunsFirstStep()
        {
            var (sut, ctx, tenantId) = CreateSut();

            var steps = new List<object>
            {
                new { Type = "Action", ActionType = "SendEmail", Config = new { To = "user@test.com" } }
            };

            var workflow = new WorkflowEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "Test Workflow",
                IsActive = true,
                Steps = JsonSerializer.Serialize(steps)
            };

            ctx.Workflows.Add(workflow);
            ctx.SaveChanges();

            var triggerEvent = new WorkflowEvent
            {
                TenantId = tenantId,
                EventName = "test.event",
                Data = new { Value = 42 }
            };

            await sut.ExecuteWorkflowAsync(workflow, triggerEvent);

            // Verify email was sent as action
            _emailServiceMock.Verify(e => e.SendSystemEmailAsync("user@test.com", "Workflow Alert", "Body"), Times.Once);

            // Verify execution log saved
            ctx.WorkflowExecutions.Should().Contain(e => e.WorkflowId == workflow.Id && e.Status == "Completed");
            ctx.WorkflowExecutionLogs.Should().Contain(l => l.StepType == "Action" && l.Status == "Success");
        }

        [Fact]
        public async Task ExecuteStepAsync_EvaluateCondition_Branching()
        {
            var (sut, ctx, tenantId) = CreateSut();

            var steps = new List<object>
            {
                new {
                    Type = "Condition",
                    Config = new {
                        Expression = "Convert.ToInt64(ctx[\"Value\"]) > 10",
                        TrueStepIndex = 2,
                        FalseStepIndex = 1
                    }
                },
                new { Type = "Action", ActionType = "WhatsApp", Config = new { To = "987654321" } },
                new { Type = "Action", ActionType = "SendSms", Config = new { To = "123456789" } }
            };

            var workflow = new WorkflowEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "Condition Test",
                IsActive = true,
                Steps = JsonSerializer.Serialize(steps)
            };

            ctx.Workflows.Add(workflow);
            ctx.SaveChanges();

            var triggerEvent = new WorkflowEvent
            {
                TenantId = tenantId,
                EventName = "test.event",
                Data = new { Value = 42 } // Evaluates True (>10)
            };

            await sut.ExecuteStepAsync(workflow.Id, 0, triggerEvent);

            // True step (SendSms) should execute
            _smsServiceMock.Verify(s => s.SendSmsAsync(tenantId, "123456789", "Workflow Msg", null), Times.Once);
            _whatsAppServiceMock.Verify(w => w.SendWhatsAppAsync(tenantId, "987654321", "Workflow WA", null), Times.Never);
        }

        [Fact]
        public async Task ExecuteStepAsync_WaitStep_SchedulesResume()
        {
            var (sut, ctx, tenantId) = CreateSut();

            var steps = new List<object>
            {
                new { Type = "Wait", Config = new { DurationMinutes = 15 } }
            };

            var workflow = new WorkflowEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "Wait Test",
                IsActive = true,
                Steps = JsonSerializer.Serialize(steps)
            };

            ctx.Workflows.Add(workflow);
            ctx.SaveChanges();

            var triggerEvent = new WorkflowEvent
            {
                TenantId = tenantId,
                EventName = "test.event",
                Data = new { }
            };

            await sut.ExecuteStepAsync(workflow.Id, 0, triggerEvent);

            // Wait action should schedule resume via backgroundJobs client
            _backgroundJobsMock.Verify(
                b => b.Create(
                    It.Is<Job>(j => j.Method.Name == "ResumeWorkflowAsync" && (int)j.Args[1] == 1),
                    It.IsAny<Hangfire.States.IState>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteCompensatoryStepsAsync_ReverseRollback()
        {
            var (sut, ctx, tenantId) = CreateSut();

            var steps = new List<object>
            {
                new {
                    Type = "Action",
                    ActionType = "AddTag",
                    Config = new { Tag = "Premium" },
                    Compensation = new { CompensationType = "UndoTag" }
                }
            };

            var workflow = new WorkflowEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = "Saga Test",
                IsActive = true,
                Steps = JsonSerializer.Serialize(steps)
            };

            ctx.Workflows.Add(workflow);
            ctx.SaveChanges();

            var executionId = Guid.NewGuid();
            ctx.WorkflowExecutions.Add(new WorkflowExecution
            {
                Id = executionId,
                TenantId = tenantId,
                WorkflowId = workflow.Id,
                Status = "Failed",
                StartedAt = DateTime.UtcNow
            });
            ctx.WorkflowExecutionLogs.Add(new WorkflowExecutionLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                WorkflowExecutionId = executionId,
                StepIndex = 0,
                StepType = "Action",
                ActionType = "AddTag",
                Status = "Success",
                ExecutedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();

            await sut.ExecuteCompensatoryStepsAsync(executionId);

            var exec = ctx.WorkflowExecutions.Find(executionId);
            exec.Should().NotBeNull();
            exec!.Status.Should().Be("Compensated");
            exec.IsCompensated.Should().BeTrue();
        }
    }
}
