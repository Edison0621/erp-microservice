using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.CRM.Domain;
using ErpSystem.CRM.Infrastructure;
using ErpSystem.BuildingBlocks.EventBus;

namespace ErpSystem.CRM.API;

[ApiController]
[Route("api/[controller]")]
public class OpportunitiesController(
    EventStoreRepository<Opportunity> repository,
    CrmReadDbContext readDb,
    IEventBus eventBus) : ControllerBase
{
    #region Queries

    /// <summary>
    /// Get all opportunities with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetOpportunities(
        [FromQuery] string? stage = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? assignedTo = null,
        [FromQuery] string? customerId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        IQueryable<OpportunityReadModel> query = readDb.Opportunities.AsQueryable();

        if (!string.IsNullOrEmpty(stage))
            query = query.Where(o => o.Stage == stage);
        if (!string.IsNullOrEmpty(priority))
            query = query.Where(o => o.Priority == priority);
        if (!string.IsNullOrEmpty(assignedTo))
            query = query.Where(o => o.AssignedToUserId == assignedTo);
        if (!string.IsNullOrEmpty(customerId))
            query = query.Where(o => o.CustomerId == customerId);

        int total = await query.CountAsync();
        List<OpportunityReadModel> items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return this.Ok(new { total, page, pageSize, items });
    }

    /// <summary>
    /// Get opportunity by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOpportunity(Guid id)
    {
        OpportunityReadModel? opportunity = await readDb.Opportunities.FindAsync(id);
        if (opportunity == null) return this.NotFound();
        return this.Ok(opportunity);
    }

    /// <summary>
    /// Get sales pipeline summary (funnel view)
    /// </summary>
    [HttpGet("pipeline")]
    public async Task<IActionResult> GetPipeline()
    {
        var pipeline = await readDb.Opportunities
            .Where(o => o.Stage != "ClosedWon" && o.Stage != "ClosedLost")
            .GroupBy(o => o.Stage)
            .Select(g => new
            {
                Stage = g.Key,
                Count = g.Count(),
                TotalValue = g.Sum(o => o.EstimatedValue),
                WeightedValue = g.Sum(o => o.WeightedValue)
            })
            .ToListAsync();

        decimal totalPipeline = pipeline.Sum(p => p.TotalValue);
        decimal totalWeighted = pipeline.Sum(p => p.WeightedValue);

        return this.Ok(new { stages = pipeline, totalPipelineValue = totalPipeline, totalWeightedValue = totalWeighted });
    }

    /// <summary>
    /// Get win/loss analysis
    /// </summary>
    [HttpGet("analysis")]
    public async Task<IActionResult> GetWinLossAnalysis([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        IQueryable<OpportunityReadModel> query = readDb.Opportunities
            .Where(o => o.Stage == "ClosedWon" || o.Stage == "ClosedLost");

        if (startDate.HasValue)
            query = query.Where(o => o.ClosedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(o => o.ClosedAt <= endDate.Value);

        int wonCount = await query.CountAsync(o => o.Stage == "ClosedWon");
        int lostCount = await query.CountAsync(o => o.Stage == "ClosedLost");
        decimal wonValue = await query.Where(o => o.Stage == "ClosedWon").SumAsync(o => o.EstimatedValue);
        decimal lostValue = await query.Where(o => o.Stage == "ClosedLost").SumAsync(o => o.EstimatedValue);

        decimal winRate = wonCount + lostCount > 0 ? (decimal)wonCount / (wonCount + lostCount) * 100 : 0;

        return this.Ok(new { wonCount, lostCount, wonValue, lostValue, winRate });
    }

    #endregion

    #region Commands

    /// <summary>
    /// Create a new opportunity
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOpportunity([FromBody] CreateOpportunityRequest request)
    {
        string oppNumber = $"OPP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        Opportunity opportunity = Opportunity.Create(
            Guid.NewGuid(),
            oppNumber,
            request.Name,
            request.LeadId,
            request.CustomerId,
            request.CustomerName,
            request.EstimatedValue,
            request.Currency ?? "CNY",
            request.ExpectedCloseDate,
            Enum.Parse<OpportunityPriority>(request.Priority ?? "Medium"),
            request.AssignedToUserId,
            request.Description);

        await repository.SaveAsync(opportunity);

        return this.CreatedAtAction(nameof(this.GetOpportunity), new { id = opportunity.Id },
            new { id = opportunity.Id, opportunityNumber = opportunity.OpportunityNumber });
    }

    /// <summary>
    /// Advance opportunity stage
    /// </summary>
    [HttpPut("{id:guid}/stage")]
    public async Task<IActionResult> AdvanceStage(Guid id, [FromBody] AdvanceStageRequest request)
    {
        Opportunity? opportunity = await repository.LoadAsync(id);
        if (opportunity == null) return this.NotFound();

        opportunity.AdvanceStage(Enum.Parse<OpportunityStage>(request.Stage), request.Notes);
        await repository.SaveAsync(opportunity);

        return this.Ok(new { id, stage = request.Stage, winProbability = opportunity.WinProbability });
    }

    /// <summary>
    /// Update opportunity value
    /// </summary>
    [HttpPut("{id:guid}/value")]
    public async Task<IActionResult> UpdateValue(Guid id, [FromBody] UpdateValueRequest request)
    {
        Opportunity? opportunity = await repository.LoadAsync(id);
        if (opportunity == null) return this.NotFound();

        opportunity.UpdateValue(request.Value, request.Reason);
        await repository.SaveAsync(opportunity);

        return this.Ok(new { id, estimatedValue = request.Value, weightedValue = opportunity.WeightedValue });
    }

    /// <summary>
    /// Mark opportunity as won
    /// </summary>
    [HttpPost("{id:guid}/won")]
    public async Task<IActionResult> MarkAsWon(Guid id, [FromBody] MarkWonRequest request)
    {
        Opportunity? opportunity = await repository.LoadAsync(id);
        if (opportunity == null) return this.NotFound();

        opportunity.MarkAsWon(request.FinalValue, request.WinReason, request.SalesOrderId);
        await repository.SaveAsync(opportunity);

        // Publish Integration Event for Sales service
        await eventBus.PublishAsync(new CrmIntegrationEvents.OpportunityWonIntegrationEvent(
            opportunity.Id,
            opportunity.OpportunityNumber,
            opportunity.Name,
            opportunity.CustomerId,
            opportunity.CustomerName,
            request.FinalValue ?? opportunity.EstimatedValue,
            opportunity.Currency,
            opportunity.AssignedToUserId,
            DateTime.UtcNow
        ));

        return this.Ok(new { id, stage = "ClosedWon", finalValue = request.FinalValue ?? opportunity.EstimatedValue });
    }

    /// <summary>
    /// Mark opportunity as lost
    /// </summary>
    [HttpPost("{id:guid}/lost")]
    public async Task<IActionResult> MarkAsLost(Guid id, [FromBody] MarkLostRequest request)
    {
        Opportunity? opportunity = await repository.LoadAsync(id);
        if (opportunity == null) return this.NotFound();

        opportunity.MarkAsLost(request.LossReason, request.CompetitorId);
        await repository.SaveAsync(opportunity);

        return this.Ok(new { id, stage = "ClosedLost", lossReason = request.LossReason });
    }

    /// <summary>
    /// Assign opportunity to user
    /// </summary>
    [HttpPut("{id:guid}/assign")]
    public async Task<IActionResult> AssignOpportunity(Guid id, [FromBody] AssignOpportunityRequest request)
    {
        Opportunity? opportunity = await repository.LoadAsync(id);
        if (opportunity == null) return this.NotFound();

        opportunity.AssignTo(request.UserId);
        await repository.SaveAsync(opportunity);

        return this.Ok(new { id, assignedTo = request.UserId });
    }

    /// <summary>
    /// Add competitor to opportunity
    /// </summary>
    [HttpPost("{id:guid}/competitors")]
    public async Task<IActionResult> AddCompetitor(Guid id, [FromBody] AddCompetitorRequest request)
    {
        Opportunity? opportunity = await repository.LoadAsync(id);
        if (opportunity == null) return this.NotFound();

        opportunity.AddCompetitor(request.CompetitorId, request.CompetitorName, request.Strengths, request.Weaknesses);
        await repository.SaveAsync(opportunity);

        return this.Ok(new { id, message = "Competitor added" });
    }

    /// <summary>
    /// Log activity for opportunity
    /// </summary>
    [HttpPost("{id:guid}/activities")]
    public async Task<IActionResult> LogActivity(Guid id, [FromBody] LogActivityRequest request)
    {
        Opportunity? opportunity = await repository.LoadAsync(id);
        if (opportunity == null) return this.NotFound();

        opportunity.LogActivity(
            request.ActivityType,
            request.Subject,
            request.Description,
            request.ActivityDate ?? DateTime.UtcNow,
            request.LoggedByUserId);

        await repository.SaveAsync(opportunity);

        return this.Ok(new { id, message = "Activity logged" });
    }

    #endregion
}

#region Request DTOs

public record CreateOpportunityRequest(
    string Name,
    Guid? LeadId,
    string? CustomerId,
    string? CustomerName,
    decimal EstimatedValue,
    string? Currency,
    DateTime ExpectedCloseDate,
    string? Priority,
    string? AssignedToUserId,
    string? Description
);

public record AdvanceStageRequest(string Stage, string? Notes);

public record UpdateValueRequest(decimal Value, string? Reason);

public record MarkWonRequest(decimal? FinalValue, string? WinReason, Guid? SalesOrderId);

public record MarkLostRequest(string LossReason, string? CompetitorId);

public record AssignOpportunityRequest(string UserId);

public record AddCompetitorRequest(
    string CompetitorId,
    string CompetitorName,
    string? Strengths,
    string? Weaknesses
);

public record LogActivityRequest(
    string ActivityType,
    string Subject,
    string Description,
    DateTime? ActivityDate,
    string LoggedByUserId
);

#endregion
