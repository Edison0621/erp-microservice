using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.CRM.Domain;
using ErpSystem.CRM.Infrastructure;

namespace ErpSystem.CRM.API;

[ApiController]
[Route("api/[controller]")]
public class CampaignsController : ControllerBase
{
    private readonly EventStoreRepository<Campaign> _repository;
    private readonly CrmReadDbContext _readDb;

    public CampaignsController(EventStoreRepository<Campaign> repository, CrmReadDbContext readDb)
    {
        _repository = repository;
        _readDb = readDb;
    }

    #region Queries

    /// <summary>
    /// Get all campaigns with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCampaigns(
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _readDb.Campaigns.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);
        if (!string.IsNullOrEmpty(type))
            query = query.Where(c => c.Type == type);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    /// <summary>
    /// Get campaign by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCampaign(Guid id)
    {
        var campaign = await _readDb.Campaigns.FindAsync(id);
        if (campaign == null) return NotFound();
        return Ok(campaign);
    }

    /// <summary>
    /// Get campaign ROI analysis
    /// </summary>
    [HttpGet("{id:guid}/roi")]
    public async Task<IActionResult> GetCampaignROI(Guid id)
    {
        var campaign = await _readDb.Campaigns.FindAsync(id);
        if (campaign == null) return NotFound();

        return Ok(new
        {
            campaignId = id,
            name = campaign.Name,
            budget = campaign.Budget,
            totalExpenses = campaign.TotalExpenses,
            budgetUtilization = campaign.Budget > 0 ? campaign.TotalExpenses / campaign.Budget * 100 : 0,
            totalLeads = campaign.TotalLeads,
            convertedLeads = campaign.ConvertedLeads,
            conversionRate = campaign.ConversionRate,
            costPerLead = campaign.CostPerLead,
            totalRevenue = campaign.TotalRevenue,
            roi = campaign.ROI
        });
    }

    /// <summary>
    /// Get all campaigns ROI summary
    /// </summary>
    [HttpGet("roi-summary")]
    public async Task<IActionResult> GetCampaignsROISummary()
    {
        var campaigns = await _readDb.Campaigns
            .Where(c => c.Status == "Completed" || c.Status == "Active")
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Type,
                c.Budget,
                c.TotalExpenses,
                c.TotalLeads,
                c.ConvertedLeads,
                c.TotalRevenue,
                c.ROI
            })
            .OrderByDescending(c => c.ROI)
            .ToListAsync();

        var totalBudget = campaigns.Sum(c => c.Budget);
        var totalExpenses = campaigns.Sum(c => c.TotalExpenses);
        var totalRevenue = campaigns.Sum(c => c.TotalRevenue);
        var overallROI = totalExpenses > 0 ? (totalRevenue - totalExpenses) / totalExpenses * 100 : 0;

        return Ok(new { campaigns, totalBudget, totalExpenses, totalRevenue, overallROI });
    }

    #endregion

    #region Commands

    /// <summary>
    /// Create a new campaign
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest request)
    {
        var campaignNumber = $"CMP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        var campaign = Campaign.Create(
            Guid.NewGuid(),
            campaignNumber,
            request.Name,
            Enum.Parse<CampaignType>(request.Type ?? "Email"),
            request.StartDate,
            request.EndDate,
            request.Budget,
            request.Currency ?? "CNY",
            request.CreatedByUserId,
            request.TargetAudience,
            request.Description);

        await _repository.SaveAsync(campaign);

        return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id },
            new { id = campaign.Id, campaignNumber = campaign.CampaignNumber });
    }

    /// <summary>
    /// Start a campaign
    /// </summary>
    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> StartCampaign(Guid id)
    {
        var campaign = await _repository.LoadAsync(id);
        if (campaign == null) return NotFound();

        campaign.Start();
        await _repository.SaveAsync(campaign);

        return Ok(new { id, status = "Active" });
    }

    /// <summary>
    /// Pause a campaign
    /// </summary>
    [HttpPost("{id:guid}/pause")]
    public async Task<IActionResult> PauseCampaign(Guid id)
    {
        var campaign = await _repository.LoadAsync(id);
        if (campaign == null) return NotFound();

        campaign.Pause();
        await _repository.SaveAsync(campaign);

        return Ok(new { id, status = "Paused" });
    }

    /// <summary>
    /// Resume a campaign
    /// </summary>
    [HttpPost("{id:guid}/resume")]
    public async Task<IActionResult> ResumeCampaign(Guid id)
    {
        var campaign = await _repository.LoadAsync(id);
        if (campaign == null) return NotFound();

        campaign.Resume();
        await _repository.SaveAsync(campaign);

        return Ok(new { id, status = "Active" });
    }

    /// <summary>
    /// Complete a campaign
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> CompleteCampaign(Guid id)
    {
        var campaign = await _repository.LoadAsync(id);
        if (campaign == null) return NotFound();

        campaign.Complete();
        await _repository.SaveAsync(campaign);

        return Ok(new { id, status = "Completed" });
    }

    /// <summary>
    /// Cancel a campaign
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelCampaign(Guid id)
    {
        var campaign = await _repository.LoadAsync(id);
        if (campaign == null) return NotFound();

        campaign.Cancel();
        await _repository.SaveAsync(campaign);

        return Ok(new { id, status = "Cancelled" });
    }

    /// <summary>
    /// Schedule a campaign
    /// </summary>
    [HttpPost("{id:guid}/schedule")]
    public async Task<IActionResult> ScheduleCampaign(Guid id)
    {
        var campaign = await _repository.LoadAsync(id);
        if (campaign == null) return NotFound();

        campaign.Schedule();
        await _repository.SaveAsync(campaign);

        return Ok(new { id, status = "Scheduled" });
    }

    /// <summary>
    /// Update campaign budget
    /// </summary>
    [HttpPut("{id:guid}/budget")]
    public async Task<IActionResult> UpdateBudget(Guid id, [FromBody] UpdateBudgetRequest request)
    {
        var campaign = await _repository.LoadAsync(id);
        if (campaign == null) return NotFound();

        campaign.UpdateBudget(request.Budget, request.Reason);
        await _repository.SaveAsync(campaign);

        return Ok(new { id, budget = request.Budget });
    }

    /// <summary>
    /// Associate a lead with this campaign
    /// </summary>
    [HttpPost("{id:guid}/leads")]
    public async Task<IActionResult> AssociateLead(Guid id, [FromBody] AssociateLeadRequest request)
    {
        var campaign = await _repository.LoadAsync(id);
        if (campaign == null) return NotFound();

        campaign.AssociateLead(request.LeadId, request.LeadNumber);
        await _repository.SaveAsync(campaign);

        return Ok(new { id, message = "Lead associated" });
    }

    /// <summary>
    /// Record an expense for the campaign
    /// </summary>
    [HttpPost("{id:guid}/expenses")]
    public async Task<IActionResult> RecordExpense(Guid id, [FromBody] RecordExpenseRequest request)
    {
        var campaign = await _repository.LoadAsync(id);
        if (campaign == null) return NotFound();

        campaign.RecordExpense(
            request.Description,
            request.Amount,
            request.ExpenseDate ?? DateTime.UtcNow,
            request.RecordedByUserId);

        await _repository.SaveAsync(campaign);

        return Ok(new { id, message = "Expense recorded" });
    }

    /// <summary>
    /// Update campaign metrics
    /// </summary>
    [HttpPut("{id:guid}/metrics")]
    public async Task<IActionResult> UpdateMetrics(Guid id, [FromBody] UpdateMetricsRequest request)
    {
        var campaign = await _repository.LoadAsync(id);
        if (campaign == null) return NotFound();

        campaign.UpdateMetrics(request.TotalLeads, request.ConvertedLeads, request.TotalRevenue);
        await _repository.SaveAsync(campaign);

        return Ok(new { id, message = "Metrics updated" });
    }

    #endregion
}

#region Request DTOs

public record CreateCampaignRequest(
    string Name,
    string? Type,
    DateTime StartDate,
    DateTime EndDate,
    decimal Budget,
    string? Currency,
    string CreatedByUserId,
    string? TargetAudience,
    string? Description
);

public record UpdateBudgetRequest(decimal Budget, string? Reason);
public record AssociateLeadRequest(Guid LeadId, string LeadNumber);

public record RecordExpenseRequest(
    string Description,
    decimal Amount,
    DateTime? ExpenseDate,
    string RecordedByUserId
);

public record UpdateMetricsRequest(int TotalLeads, int ConvertedLeads, decimal TotalRevenue);

#endregion
