using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using Upkilo.Infrastructure.Data;

namespace Upkilo.API.Controllers;

/// <summary>
/// Sales pipeline controller for CRM deal tracking and management.
/// Supports pipeline CRUD, deal management, stage transitions, and activity logging.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class SalesPipelineController : ControllerBase
{
    private readonly ILogger<SalesPipelineController> _logger;
    private readonly AppDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IEventService _eventService;

    public SalesPipelineController(
        ILogger<SalesPipelineController> logger,
        AppDbContext context,
        ITenantProvider tenantProvider,
        IEventService eventService)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
        _eventService = eventService;
    }

    // ── Pipeline Endpoints ────────────────────────────────────────────────

    /// <summary>
    /// Get all sales pipelines for this tenant
    /// </summary>
    [HttpGet("pipelines")]
    public async Task<IActionResult> GetPipelines()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var pipelines = await _context.SalesPipelines
            .Where(p => p.TenantId == tenantId.Value && !p.IsDeleted)
            .Include(p => p.Stages.Where(s => !s.IsDeleted).OrderBy(s => s.DisplayOrder))
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.IsDefault,
                p.IsActive,
                stageCount = p.Stages.Count(s => !s.IsDeleted),
                dealCount = p.Deals.Count(d => !d.IsDeleted && d.Status == DealStatus.Open),
                totalValue = (decimal)p.Deals.Where(d => !d.IsDeleted && d.Status == DealStatus.Open).Sum(d => (double)d.Value),
                stages = p.Stages.Where(s => !s.IsDeleted).OrderBy(s => s.DisplayOrder).Select(s => new
                {
                    s.Id,
                    s.Name,
                    SortOrder = s.DisplayOrder,
                    WinProbability = s.WinProbability * 100m,
                    s.Color
                }),
                p.CreatedAt
            })
            .ToListAsync();

        return Ok(new { data = pipelines });
    }

    /// <summary>
    /// Create a new pipeline with stages
    /// </summary>
    [HttpPost("pipelines")]
    public async Task<IActionResult> CreatePipeline([FromBody] CreatePipelineRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Pipeline name is required." });

        // If this is the first pipeline or marked as default, set it
        var hasExisting = await _context.SalesPipelines
            .AnyAsync(p => p.TenantId == tenantId.Value && !p.IsDeleted);

        var pipeline = new SalesPipeline
        {
            TenantId = tenantId.Value,
            Name = request.Name,
            IsDefault = !hasExisting || request.IsDefault,
            IsActive = true
        };

        _context.SalesPipelines.Add(pipeline);

        // Add default stages if none provided
        var stages = request.Stages?.Any() == true
            ? request.Stages
            : new List<CreateStageRequest>
            {
                new("Lead", 0, 10, "#6B7280"),
                new("Qualified", 1, 25, "#3B82F6"),
                new("Proposal", 2, 50, "#8B5CF6"),
                new("Negotiation", 3, 75, "#F59E0B"),
                new("Closed Won", 4, 100, "#10B981"),
                new("Closed Lost", 5, 0, "#EF4444"),
            };

        foreach (var stageReq in stages)
        {
            _context.PipelineStages.Add(new PipelineStage
            {
                TenantId = tenantId.Value,
                PipelineId = pipeline.Id,
                Name = stageReq.Name,
                DisplayOrder = stageReq.SortOrder,
                WinProbability = stageReq.WinProbability / 100m,
                Color = stageReq.Color ?? "#3B82F6"
            });
        }

        // If setting as default, unset others
        if (pipeline.IsDefault)
        {
            await _context.SalesPipelines
                .Where(p => p.TenantId == tenantId.Value && p.Id != pipeline.Id)
                .ExecuteUpdateAsync(p => p.SetProperty(x => x.IsDefault, false));
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Pipeline created: {PipelineId} - {Name}", pipeline.Id, pipeline.Name);

        return CreatedAtAction(nameof(GetPipelines), null, new { pipeline.Id, pipeline.Name, pipeline.IsDefault });
    }

    /// <summary>
    /// Create a new stage for a pipeline
    /// </summary>
    [HttpPost("pipelines/{pipelineId}/stages")]
    public async Task<IActionResult> CreatePipelineStage(Guid pipelineId, [FromBody] CreateStageRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var pipeline = await _context.SalesPipelines
            .FirstOrDefaultAsync(p => p.Id == pipelineId && p.TenantId == tenantId.Value && !p.IsDeleted);

        if (pipeline == null) return NotFound("Pipeline not found.");

        var newStage = new PipelineStage
        {
            TenantId = tenantId.Value,
            PipelineId = pipelineId,
            Name = request.Name,
            DisplayOrder = request.SortOrder,
            WinProbability = request.WinProbability / 100m,
            Color = request.Color ?? "#3B82F6"
        };

        _context.PipelineStages.Add(newStage);
        await _context.SaveChangesAsync();

        return Created($"/api/v1/salespipeline/pipelines/{pipelineId}/stages/{newStage.Id}", new
        {
            newStage.Id,
            newStage.Name,
            SortOrder = newStage.DisplayOrder,
            WinProbability = newStage.WinProbability * 100m,
            newStage.Color
        });
    }

    /// <summary>
    /// Reorder pipeline stages
    /// </summary>
    [HttpPut("pipelines/{pipelineId}/stages/reorder")]
    public async Task<IActionResult> ReorderPipelineStages(Guid pipelineId, [FromBody] ReorderStagesRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var stages = await _context.PipelineStages
            .Where(s => s.PipelineId == pipelineId && s.TenantId == tenantId.Value && !s.IsDeleted)
            .ToListAsync();

        if (!stages.Any()) return NotFound("Pipeline not found or has no stages.");

        foreach (var orderReq in request.StageOrders)
        {
            var stage = stages.FirstOrDefault(s => s.Id == orderReq.StageId);
            if (stage != null)
            {
                stage.DisplayOrder = orderReq.SortOrder;
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    // ── Deal Endpoints ────────────────────────────────────────────────────

    /// <summary>
    /// Get all deals for a pipeline (Kanban board data)
    /// </summary>
    [HttpGet("pipelines/{pipelineId}/deals")]
    public async Task<IActionResult> GetDeals(
        Guid pipelineId,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 50)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var query = _context.Deals
            .Where(d => d.PipelineId == pipelineId && d.TenantId == tenantId.Value && !d.IsDeleted);

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<DealStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(d => d.Status == parsedStatus);
            }
            else
            {
                return BadRequest(new { error = "Invalid status value." });
            }
        }

        var total = await query.CountAsync();

        var deals = await query
            .Include(d => d.Stage)
            .Include(d => d.Client)
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(d => new
            {
                d.Id,
                d.Title,
                d.Value,
                d.Currency,
                d.Status,
                stage = d.Stage != null ? new { d.Stage.Id, d.Stage.Name, d.Stage.Color, WinProbability = d.Stage.WinProbability * 100m } : null,
                client = d.Client != null ? new { d.Client.Id, name = d.Client.FirstName + " " + d.Client.LastName, d.Client.Email } : null,
                d.ExpectedCloseDate,
                AssignedToId = d.AssignedToStaffId,
                d.CreatedAt
            })
            .ToListAsync();

        // Summary by stage for the pipeline
        var stagesSummary = await _context.PipelineStages
            .Where(s => s.PipelineId == pipelineId && s.TenantId == tenantId.Value && !s.IsDeleted)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Color,
                SortOrder = s.DisplayOrder,
                WinProbability = s.WinProbability * 100m,
                dealCount = _context.Deals.Count(d => d.StageId == s.Id && !d.IsDeleted && d.Status == DealStatus.Open),
                totalValue = (decimal)_context.Deals.Where(d => d.StageId == s.Id && !d.IsDeleted && d.Status == DealStatus.Open).Sum(d => (double)d.Value)
            })
            .ToListAsync();

        return Ok(new { data = deals, stages = stagesSummary, total, page, limit });
    }

    /// <summary>
    /// Create a new deal
    /// </summary>
    [HttpPost("deals")]
    public async Task<IActionResult> CreateDeal([FromBody] CreateDealRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { error = "Deal title is required." });

        // Verify the pipeline and stage exist
        var stage = await _context.PipelineStages
            .FirstOrDefaultAsync(s => s.Id == request.StageId && s.TenantId == tenantId.Value && !s.IsDeleted);

        if (stage == null)
            return BadRequest(new { error = "Invalid stage ID." });

        var deal = new Deal
        {
            TenantId = tenantId.Value,
            PipelineId = stage.PipelineId,
            StageId = request.StageId,
            ClientId = request.ClientId,
            Title = request.Title,
            Value = request.Value,
            Currency = request.Currency ?? "USD",
            Status = DealStatus.Open,
            ExpectedCloseDate = request.ExpectedCloseDate,
            AssignedToStaffId = request.AssignedToId
        };

        _context.Deals.Add(deal);

        // Log activity
        _context.DealActivities.Add(new DealActivity
        {
            TenantId = tenantId.Value,
            DealId = deal.Id,
            ActivityType = "Created",
            Description = $"Deal \"{deal.Title}\" created in stage \"{stage.Name}\"",
            PerformedById = GetCurrentUserId()
        });

        await _context.SaveChangesAsync();

        await _eventService.PublishAsync("deal.created", deal, tenantId.Value);

        _logger.LogInformation("Deal created: {DealId} - {Title} in pipeline {PipelineId}", deal.Id, deal.Title, deal.PipelineId);

        return CreatedAtAction(nameof(GetDeal), new { id = deal.Id }, new { deal.Id, deal.Title, deal.Value, deal.Status });
    }

    /// <summary>
    /// Get a deal by ID with full details
    /// </summary>
    [HttpGet("deals/{id}")]
    public async Task<IActionResult> GetDeal(Guid id)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var deal = await _context.Deals
            .Include(d => d.Stage)
            .Include(d => d.Client)
            .Include(d => d.Pipeline)
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId.Value && !d.IsDeleted);

        if (deal == null) return NotFound();

        return Ok(new
        {
            deal.Id,
            deal.Title,
            deal.Value,
            deal.Currency,
            deal.Status,
            pipeline = deal.Pipeline != null ? new { deal.Pipeline.Id, deal.Pipeline.Name } : null,
            stage = deal.Stage != null ? new { deal.Stage.Id, deal.Stage.Name, deal.Stage.Color, deal.Stage.WinProbability } : null,
            client = deal.Client != null ? new { deal.Client.Id, name = deal.Client.FirstName + " " + deal.Client.LastName, deal.Client.Email, deal.Client.Phone } : null,
            deal.ExpectedCloseDate,
            ActualCloseDate = deal.ActualCloseDate,
            LostReason = deal.LostReason,
            AssignedToId = deal.AssignedToStaffId,
            activities = (await _context.DealActivities.Where(a => a.DealId == deal.Id && !a.IsDeleted).OrderByDescending(a => a.CreatedAt).Take(20).ToListAsync()).Select(a => new
            {
                a.Id,
                a.ActivityType,
                a.Description,
                a.PerformedById,
                a.Metadata,
                a.CreatedAt
            }),
            deal.CreatedAt,
            deal.UpdatedAt
        });
    }

    /// <summary>
    /// Move a deal to a different stage
    /// </summary>
    [HttpPut("deals/{id}/move")]
    public async Task<IActionResult> MoveDeal(Guid id, [FromBody] MoveDealRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var deal = await _context.Deals
            .Include(d => d.Stage)
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId.Value && !d.IsDeleted);

        if (deal == null) return NotFound();

        var newStage = await _context.PipelineStages
            .FirstOrDefaultAsync(s => s.Id == request.StageId && s.TenantId == tenantId.Value && !s.IsDeleted);

        if (newStage == null)
            return BadRequest(new { error = "Invalid stage ID." });

        var oldStageName = deal.Stage?.Name ?? "Unknown";
        deal.StageId = request.StageId;
        deal.UpdatedAt = DateTime.UtcNow;

        // Auto-close if stage win probability is 100% (1.0) or 0%
        if (newStage.WinProbability >= 1.0m)
        {
            deal.Status = DealStatus.Won;
            deal.ActualCloseDate = DateTime.UtcNow;
        }
        else if (newStage.WinProbability <= 0m && newStage.Name.Contains("Lost", StringComparison.OrdinalIgnoreCase))
        {
            deal.Status = DealStatus.Lost;
            deal.ActualCloseDate = DateTime.UtcNow;
            deal.LostReason = request.Reason;
        }

        // Log the stage change
        _context.DealActivities.Add(new DealActivity
        {
            TenantId = tenantId.Value,
            DealId = deal.Id,
            ActivityType = "StageChanged",
            Description = $"Moved from \"{oldStageName}\" to \"{newStage.Name}\"",
            PerformedById = GetCurrentUserId()
        });

        await _context.SaveChangesAsync();

        await _eventService.PublishAsync("deal.stage_changed", new { deal.Id, OldStage = oldStageName, NewStage = newStage.Name, deal.Status }, tenantId.Value);

        _logger.LogInformation("Deal {DealId} moved from {OldStage} to {NewStage}", id, oldStageName, newStage.Name);

        return Ok(new { deal.Id, deal.Status, stage = new { newStage.Id, newStage.Name } });
    }

    /// <summary>
    /// Add an activity note to a deal
    /// </summary>
    [HttpPost("deals/{id}/activities")]
    public async Task<IActionResult> AddActivity(Guid id, [FromBody] AddDealActivityRequest request)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var deal = await _context.Deals
            .FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantId.Value && !d.IsDeleted);

        if (deal == null) return NotFound();

        var activity = new DealActivity
        {
            TenantId = tenantId.Value,
            DealId = id,
            ActivityType = request.Type,
            Description = request.Description,
            PerformedById = GetCurrentUserId(),
            Metadata = request.Metadata
        };

        _context.DealActivities.Add(activity);
        deal.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { activity.Id, activity.ActivityType, activity.Description, activity.CreatedAt });
    }

    /// <summary>
    /// Export deals to CSV
    /// </summary>
    [HttpGet("pipelines/{pipelineId}/deals/export")]
    public async Task<IActionResult> ExportDeals(Guid pipelineId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var deals = await _context.Deals
            .Include(d => d.Stage)
            .Include(d => d.Client)
            .Where(d => d.PipelineId == pipelineId && d.TenantId == tenantId.Value && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("Deal Title,Value,Currency,Status,Stage,Client Name,Client Email,Expected Close Date,Created At");

        foreach (var d in deals)
        {
            var clientName = d.Client != null ? $"{d.Client.FirstName} {d.Client.LastName}" : "";
            var clientEmail = d.Client?.Email ?? "";
            var stageName = d.Stage?.Name ?? "";

            builder.AppendLine($"\"{d.Title}\",{d.Value},{d.Currency},{d.Status},\"{stageName}\",\"{clientName}\",\"{clientEmail}\",{d.ExpectedCloseDate?.ToString("yyyy-MM-dd")},{d.CreatedAt:yyyy-MM-dd}");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(builder.ToString());
        return File(bytes, "text/csv", $"deals_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
    }

    /// <summary>
    /// Get pipeline analytics (win rates, velocity, etc.)
    /// </summary>
    [HttpGet("pipelines/{pipelineId}/analytics")]
    public async Task<IActionResult> GetPipelineAnalytics(Guid pipelineId)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var deals = await _context.Deals
            .Where(d => d.PipelineId == pipelineId && d.TenantId == tenantId.Value && !d.IsDeleted)
            .ToListAsync();

        var totalDeals = deals.Count;
        var wonDeals = deals.Count(d => d.Status == DealStatus.Won);
        var lostDeals = deals.Count(d => d.Status == DealStatus.Lost);
        var openDeals = deals.Count(d => d.Status == DealStatus.Open);

        var wonDealValues = deals.Where(d => d.Status == DealStatus.Won);
        var avgDealSize = wonDealValues.Any() ? wonDealValues.Average(d => d.Value) : 0;
        var totalRevenue = wonDealValues.Sum(d => d.Value);

        var closedDeals = deals.Where(d => d.ActualCloseDate.HasValue);
        var avgCycleTimeDays = closedDeals.Any()
            ? closedDeals.Average(d => (d.ActualCloseDate!.Value - d.CreatedAt).TotalDays)
            : 0;

        return Ok(new
        {
            totalDeals,
            openDeals,
            wonDeals,
            lostDeals,
            winRate = totalDeals > 0 ? Math.Round((double)wonDeals / (wonDeals + lostDeals) * 100, 1) : 0,
            openValue = deals.Where(d => d.Status == DealStatus.Open).Sum(d => d.Value),
            totalRevenue,
            avgDealSize = Math.Round(avgDealSize, 2),
            avgCycleTimeDays = Math.Round(avgCycleTimeDays, 1)
        });
    }

    /// <summary>
    /// Get cross-pipeline dashboard summary with KPIs, monthly trend, and top deals
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetPipelineDashboard()
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null) return Unauthorized();

        var deals = await _context.Deals
            .Where(d => d.TenantId == tenantId.Value && !d.IsDeleted)
            .Include(d => d.Stage)
            .ToListAsync();

        var totalDeals = deals.Count;
        var openDeals = deals.Where(d => d.Status == DealStatus.Open).ToList();
        var wonDeals = deals.Where(d => d.Status == DealStatus.Won).ToList();
        var lostDeals = deals.Where(d => d.Status == DealStatus.Lost).ToList();

        // Weighted pipeline value: sum of (deal value * stage win probability)
        var weightedPipelineValue = openDeals
            .Where(d => d.Stage != null)
            .Sum(d => d.Value * d.Stage!.WinProbability);

        // Monthly trend (last 6 months)
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var monthlyTrend = deals
            .Where(d => d.CreatedAt >= sixMonthsAgo)
            .GroupBy(d => new { d.CreatedAt.Year, d.CreatedAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new
            {
                month = $"{g.Key.Year}-{g.Key.Month:D2}",
                created = g.Count(),
                won = g.Count(d => d.Status == DealStatus.Won),
                lost = g.Count(d => d.Status == DealStatus.Lost),
                revenue = g.Where(d => d.Status == DealStatus.Won).Sum(d => d.Value)
            })
            .ToList();

        // Top 5 open deals by value
        var topDeals = openDeals
            .OrderByDescending(d => d.Value)
            .Take(5)
            .Select(d => new
            {
                d.Id,
                d.Title,
                d.Value,
                stageName = d.Stage?.Name ?? "Unknown",
                d.ExpectedCloseDate
            })
            .ToList();

        // Avg deal cycle time
        var closedDeals = deals.Where(d => d.ActualCloseDate.HasValue).ToList();
        var avgCycleDays = closedDeals.Any()
            ? Math.Round(closedDeals.Average(d => (d.ActualCloseDate!.Value - d.CreatedAt).TotalDays), 1)
            : 0;

        return Ok(new
        {
            kpis = new
            {
                totalDeals,
                openDeals = openDeals.Count,
                wonDeals = wonDeals.Count,
                lostDeals = lostDeals.Count,
                winRate = (wonDeals.Count + lostDeals.Count) > 0
                    ? Math.Round((double)wonDeals.Count / (wonDeals.Count + lostDeals.Count) * 100, 1)
                    : 0,
                openValue = openDeals.Sum(d => d.Value),
                weightedPipelineValue = Math.Round(weightedPipelineValue, 2),
                totalRevenue = wonDeals.Sum(d => d.Value),
                avgCycleDays
            },
            monthlyTrend,
            topDeals
        });
    }

    private Guid? GetCurrentUserId()
    {
        var idClaim = User.FindFirst("id")?.Value ?? (User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        return Guid.TryParse(idClaim, out var id) ? id : null;
    }

    // ── Request DTOs (nested to avoid duplicate namespace-level definitions) ──
    public record CreatePipelineRequest(string Name, bool IsDefault = false, List<CreateStageRequest>? Stages = null);
    public record CreateStageRequest(string Name, int SortOrder, decimal WinProbability, string? Color = null);
    public record CreateDealRequest(string Title, Guid StageId, Guid? ClientId, decimal Value, string? Currency, DateTime? ExpectedCloseDate, Guid? AssignedToId);
    public record MoveDealRequest(Guid StageId, string? Reason = null);
    public record AddDealActivityRequest(string Type, string Description, string? Metadata = null);
    public record ReorderStagesRequest(List<StageOrderRequest> StageOrders);
    public record StageOrderRequest(Guid StageId, int SortOrder);

}

