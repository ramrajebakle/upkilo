using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Core.Interfaces.Workflow;
using Upkilo.Infrastructure.Services;
using Upkilo.Tests.Helpers;
using Xunit;

namespace Upkilo.Tests.Services;

public class WorkflowStepExecutorTests : IDisposable
{
    private readonly TestDbContextFactory _dbFactory;
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<ISmsService> _smsServiceMock = new();
    private readonly Mock<IWebhookService> _webhookServiceMock = new();
    private readonly Mock<ILogger<WorkflowStepExecutor>> _loggerMock = new();
    private readonly Mock<HttpMessageHandler> _httpHandlerMock = new();
    private readonly SlackNotificationService _slackService;
    private readonly DiscordNotificationService _discordService;

    public WorkflowStepExecutorTests()
    {
        _dbFactory = new TestDbContextFactory();

        var client = new HttpClient(_httpHandlerMock.Object);
        _httpHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("ok")
            });

        _slackService = new SlackNotificationService(client, new Mock<ILogger<SlackNotificationService>>().Object);
        _discordService = new DiscordNotificationService(client, new Mock<ILogger<DiscordNotificationService>>().Object);
    }

    public void Dispose() => _dbFactory.Dispose();

    private WorkflowStepExecutor CreateSut()
    {
        return new WorkflowStepExecutor(
            _emailServiceMock.Object,
            _smsServiceMock.Object,
            _webhookServiceMock.Object,
            _slackService,
            _discordService,
            _dbFactory.CreateContext(),
            _loggerMock.Object
        );
    }

    private class ConcreteStepConfig : IWorkflowStepConfig
    {
        public string StepName { get; set; } = "Test Step";
        public string StepType { get; set; } = "email";
        public string To { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Body { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Message { get; set; } = "";
        public string EventType { get; set; } = "";
        public string WebhookUrl { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Priority { get; set; } = "";
        public string AssignedTo { get; set; } = "";
    }

    [Fact]
    public async Task ExecuteAsync_EmailType_SendsEmailWithReplacedVars()
    {
        var sut = CreateSut();
        var context = new WorkflowContext
        {
            TenantId = Guid.NewGuid(),
            State = new Dictionary<string, object> { { "ClientName", "John Doe" } }
        };

        var config = new ConcreteStepConfig
        {
            StepType = "email",
            To = "john@example.com",
            Subject = "Welcome {ClientName}",
            Body = "Hello {ClientName}, glad to have you!"
        };

        _emailServiceMock.Setup(e => e.SendEmailAsync("john@example.com", "Welcome John Doe", "Hello John Doe, glad to have you!", It.IsAny<bool>(), It.IsAny<List<(string, byte[])>?>()))
            .Returns(Task.CompletedTask);

        var result = await sut.ExecuteAsync(config, context);

        result.Success.Should().BeTrue();
        _emailServiceMock.Verify(e => e.SendEmailAsync("john@example.com", "Welcome John Doe", "Hello John Doe, glad to have you!", It.IsAny<bool>(), It.IsAny<List<(string, byte[])>?>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SmsType_SendsSms()
    {
        var sut = CreateSut();
        var context = new WorkflowContext
        {
            TenantId = Guid.NewGuid(),
            State = new Dictionary<string, object> { { "Code", "12345" } }
        };

        var config = new ConcreteStepConfig
        {
            StepType = "sms",
            PhoneNumber = "+1234567890",
            Message = "Your code is {Code}"
        };

        _smsServiceMock.Setup(s => s.SendSmsAsync(context.TenantId, "+1234567890", "Your code is 12345", null))
            .ReturnsAsync(new SmsResult(true, "id", null));

        var result = await sut.ExecuteAsync(config, context);

        result.Success.Should().BeTrue();
        _smsServiceMock.Verify(s => s.SendSmsAsync(context.TenantId, "+1234567890", "Your code is 12345", null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WebhookType_DispatchesEvent()
    {
        var sut = CreateSut();
        var context = new WorkflowContext
        {
            TenantId = Guid.NewGuid(),
            State = new Dictionary<string, object>()
        };

        var config = new ConcreteStepConfig
        {
            StepType = "webhook",
            EventType = "my.custom.event"
        };

        var result = await sut.ExecuteAsync(config, context);

        result.Success.Should().BeTrue();
        _webhookServiceMock.Verify(w => w.DispatchEventAsync(context.TenantId, "my.custom.event", context.State), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SlackType_SendsNotification()
    {
        var sut = CreateSut();
        var context = new WorkflowContext
        {
            TenantId = Guid.NewGuid(),
            State = new Dictionary<string, object> { { "User", "Alice" } }
        };

        var config = new ConcreteStepConfig
        {
            StepType = "slack",
            WebhookUrl = "https://slack.com/hook",
            Message = "{User} joined"
        };

        var result = await sut.ExecuteAsync(config, context);

        result.Success.Should().BeTrue();
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == "https://slack.com/hook"),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task ExecuteAsync_DiscordType_SendsNotification()
    {
        var sut = CreateSut();
        var context = new WorkflowContext
        {
            TenantId = Guid.NewGuid(),
            State = new Dictionary<string, object> { { "User", "Bob" } }
        };

        var config = new ConcreteStepConfig
        {
            StepType = "discord",
            WebhookUrl = "https://discord.com/hook",
            Message = "{User} posted"
        };

        var result = await sut.ExecuteAsync(config, context);

        result.Success.Should().BeTrue();
        _httpHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == "https://discord.com/hook"),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task ExecuteAsync_CreateTaskType_PersistsTask()
    {
        var dbContext = _dbFactory.CreateContext();
        var sut = new WorkflowStepExecutor(
            _emailServiceMock.Object,
            _smsServiceMock.Object,
            _webhookServiceMock.Object,
            _slackService,
            _discordService,
            dbContext,
            _loggerMock.Object
        );

        var tenantId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var context = new WorkflowContext
        {
            TenantId = tenantId,
            State = new Dictionary<string, object> { { "Name", "Charlie" } }
        };

        var config = new ConcreteStepConfig
        {
            StepType = "create_task",
            Title = "Task for {Name}",
            Description = "Call {Name} ASAP",
            Priority = "High",
            AssignedTo = staffId.ToString()
        };

        var result = await sut.ExecuteAsync(config, context);

        result.Success.Should().BeTrue();

        var task = dbContext.Set<CrmTask>().FirstOrDefault(t => t.TenantId == tenantId);
        task.Should().NotBeNull();
        task!.Title.Should().Be("Task for Charlie");
        task.Description.Should().Be("Call Charlie ASAP");
        task.Priority.Should().Be("High");
        task.AssignedTo.Should().Be(staffId);
        task.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task CompensateAsync_CreateTaskType_DeletesTask()
    {
        var dbContext = _dbFactory.CreateContext();
        var tenantId = Guid.NewGuid();
        var task = new CrmTask
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = "Do not forget",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Set<CrmTask>().Add(task);
        await dbContext.SaveChangesAsync();

        var sut = new WorkflowStepExecutor(
            _emailServiceMock.Object,
            _smsServiceMock.Object,
            _webhookServiceMock.Object,
            _slackService,
            _discordService,
            dbContext,
            _loggerMock.Object
        );

        var context = new WorkflowContext
        {
            TenantId = tenantId,
            State = new Dictionary<string, object> { { "compensate_task_id", task.Id.ToString() } }
        };

        var config = new ConcreteStepConfig { StepType = "create_task" };

        await sut.CompensateAsync(config, context);

        dbContext.ChangeTracker.Clear();
        dbContext.Set<CrmTask>().Find(task.Id).Should().BeNull();
    }

    [Fact]
    public async Task CompensateAsync_WebhookType_DispatchesCompensatedEvent()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();
        var context = new WorkflowContext
        {
            TenantId = tenantId,
            State = new Dictionary<string, object> { { "Info", "SomeInfo" } }
        };

        var config = new ConcreteStepConfig
        {
            StepType = "webhook",
            EventType = "my.event"
        };

        await sut.CompensateAsync(config, context);

        _webhookServiceMock.Verify(w => w.DispatchEventAsync(tenantId, "my.event.compensated", context.State), Times.Once);
    }
}
