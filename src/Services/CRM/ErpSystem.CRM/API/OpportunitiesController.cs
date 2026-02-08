using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.CRM.Domain;
using ErpSystem.CRM.Infrastructure;

namespace ErpSystem.CRM.API;

[ApiController]
[Route("api/[controller]")]
public class OpportunitiesController : ControllerBase
{
    private readonly EventStoreRepository<Opportunity> _repository;
    private readonly CrmReadDbContext _readDb;

    public OpportunitiesController(EventStoreRepository<Opportunity> repository, CrmReadDbContext readDb)
    {
        _repository = repository;
        _readDb = readDb;
    }

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
        var query = _readDb.Opportunities.AsQueryable();

        if (!string.IsNullOrEmpty(stage))
            query = query.Where(o => o.Stage == stage);
        if (!string.IsNullOrEmpty(priority))
            query = query.Where(o => o.Priority == priority);
        if (!string.IsNullOrEmpty(assignedTo))
            query = query.Where(o => o.AssignedToUserId == assignedTo);
        if (!string.IsNullOrEmpty(customerId))
            query = query.Where(o => o.CustomerId == customerId);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    /// <summary>
    /// Get opportunity by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOpportunity(Guid id)
    {
        var opportunity = await _readDb.Opportunities.FindAsync(id);
        if (opportunity == null) return NotFound();
        return Ok(opportunity);
    }

    /// <summary>
    /// Get sales pipeline summary (funnel view)
    /// </summary>
    [HttpGet("pipeline")]
    public async Task<IActionResult> GetPipeline()
    {
        var pipeline = await _readDb.Opportunities
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

        var totalPipeline = pipeline.Sum(p => p.TotalValue);
        var totalWeighted = pipeline.Sum(p => p.WeightedValue);

        return Ok(new { stages = pipeline, totalPipelineValue = totalPipeline, totalWeightedValue = totalWeighted });
    }

    /// <summary>
    /// Get win/loss analysis
    /// </summary>
    [HttpGet("analysis")]
    public async Task<IActionResult> GetWinLossAnalysis([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var query = _readDb.Opportunities
            .Where(o => o.Stage == "ClosedWon" || o.Stage == "ClosedLost");

        if (startDate.HasValue)
            query = query.Where(o => o.ClosedAt >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(o => o.ClosedAt <= endDate.Value);

        var wonCount = await query.CountAsync(o => o.Stage == "ClosedWon");
        var lostCount = await query.CountAsync(o => o.Stage == "ClosedLost");
        var wonValue = await query.Where(o => o.Stage == "ClosedWon").SumAsync(o => o.EstimatedValue);
        var lostValue = await query.Where(o => o.Stage == "ClosedLost").SumAsync(o => o.EstimatedValue);

        var winRate = wonCount + lostCount > 0 ? (decimal)wonCount / (wonCount + lostCount) * 100 : 0;

        return Ok(new { wonCount, lostCount, wonValue, lostValue, winRate });
    }

    #endregion

    #region Commands

    /// <summary>
    /// Create a new opportunity
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOpportunity([FromBody] CreateOpportunityRequest request)
    {
        var oppNumber = $"OPP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        var opportunity = Opportunity.Create(
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

        await _repository.SaveAsync(opportunity);

        return CreatedAtAction(nameof(GetOpportunity), new { id = opportunity.Id },
            new { id = opportunity.Id, opportunityNumber = opportunity.OpportunityNumber });
    }

    /// <summary>
    /// Advance opportunity stage
    /// </summary>
    [HttpPut("{id:guid}/stage")]
    public async Task<IActionResult> AdvanceStage(Guid id, [FromBody] AdvanceStageRequest request)
    {
        var opportunity = await _repository.LoadAsync(id);
        if (opportunity == null) return NotFound();

        opportunity.AdvanceStage(Enum.Parse<OpportunityStage>(request.Stage), request.Notes);
        await _repository.SaveAsync(opportunity);

        return Ok(new { id, stage = request.Stage, winProbability = opportunity.WinProbability });
    }

    /// <summary>
    /// Update opportunity value
    /// </summary>
    [HttpPut("{id:guid}/value")]
    public async Task<IActionResult> UpdateValue(Guid id, [FromBody] UpdateValueRequest request)
    {
        var opportunity = await _repository.LoadAsync(id);
        if (opportunity == null) return NotFound();

        opportunity.UpdateValue(request.Value, request.Reason);
        await _repository.SaveAsync(opportunity);

        return Ok(new { id, estimatedValue = request.Value, weightedValue = opportunity.WeightedValue });
    }

    /// <summary>
    /// Mark opportunity as won
    /// </summary>
    [HttpPost("{id:guid}/won")]
    public async Task<IActionResult> MarkAsWon(Guid id, [FromBody] MarkWonRequest request)
    {
        var opportunity = await _repository.LoadAsync(id);
        if (opportunity == null) return NotFound();

        opportunity.MarkAsWon(request.FinalValue, request.WinReason, request.SalesOrderId);
        await _repository.SaveAsync(opportunity);

        return Ok(new { id, stage = "ClosedWon", finalValue = request.FinalValue ?? opportunity.EstimatedValue });
    }

    /// <summary>
    /// Mark opportunity as lost
    /// </summary>
    [HttpPost("{id:guid}/lost")]
    public async Task<IActionResult> MarkAsLost(Guid id, [FromBody] MarkLostRequest request)
    {
        var opportunity = await _repository.LoadAsync(id);
        if (opportunity == null) return NotFound();

        opportunity.MarkAsLost(request.LossReason, request.CompetitorId);
        await _repository.SaveAsync(opportunity);

        return Ok(new { id, stage = "ClosedLost", lossReason = request.LossReason });
    }

    /// <summary>
    /// Assign opportunity to user
    /// </summary>
    [HttpPut("{id:guid}/assign")]
    public async Task<IActionResult> AssignOpportunity(Guid id, [FromBody] AssignOpportunityRequest request)
    {
        var opportunity = await _repository.LoadAsync(id);
        if (opportunity == null) return NotFound();

        opportunity.AssignTo(request.UserId);
        await _repository.SaveAsync(opportunity);

        return Ok(new { id, assignedTo = request.UserId });
    }

    /// <summary>
    /// Add competitor to opportunity
    /// </summary>
    [HttpPost("{id:guid}/competitors")]
    public async Task<IActionResult> AddCompetitor(Guid id, [FromBody] AddCompetitorRequest request)
    {
        var opportunity = await _repository.LoadAsync(id);
        if (opportunity == null) return NotFound();

        opportunity.AddCompetitor(request.CompetitorId, request.CompetitorName, request.Strengths, request.Weaknesses);
        await _repository.SaveAsync(opportunity);

        return Ok(new { id, message = "Competitor added" });
    }

    /// <summary>
    /// Log activity for opportunity
    /// </summary>
    [HttpPost("{id:guid}/activities")]
    public async Task<IActionResult> LogActivity(Guid id, [FromBody] LogActivityRequest request)
    {
        var opportunity = await _repository.LoadAsync(id);
        if (opportunity == null) return NotFound();

        opportunity.LogActivity(
            request.ActivityType,
            request.Subject,
            request.Description,
            request.ActivityDate ?? DateTime.UtcNow,
            request.LoggedByUserId);

        await _repository.SaveAsync(opportunity);

        return Ok(new { id, message = "Activity logged" });
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
