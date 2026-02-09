using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.CRM.Domain;
using ErpSystem.CRM.Infrastructure;

namespace ErpSystem.CRM.API;

[ApiController]
[Route("api/[controller]")]
public class CampaignsController(EventStoreRepository<Campaign> repository, CrmReadDbContext readDb) : ControllerBase
{
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
        IQueryable<CampaignReadModel> query = readDb.Campaigns.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(c => c.Status == status);
        if (!string.IsNullOrEmpty(type))
            query = query.Where(c => c.Type == type);

        int total = await query.CountAsync();
        List<CampaignReadModel> items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return this.Ok(new { total, page, pageSize, items });
    }

    /// <summary>
    /// Get campaign by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCampaign(Guid id)
    {
        CampaignReadModel? campaign = await readDb.Campaigns.FindAsync(id);
        if (campaign == null) return this.NotFound();
        return this.Ok(campaign);
    }

    /// <summary>
    /// Get campaign ROI analysis
    /// </summary>
    [HttpGet("{id:guid}/roi")]
    public async Task<IActionResult> GetCampaignRoi(Guid id)
    {
        CampaignReadModel? campaign = await readDb.Campaigns.FindAsync(id);
        if (campaign == null) return this.NotFound();

        return this.Ok(new
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
            roi = campaign.Roi
        });
    }

    /// <summary>
    /// Get all campaigns ROI summary
    /// </summary>
    [HttpGet("roi-summary")]
    public async Task<IActionResult> GetCampaignsRoiSummary()
    {
        var campaigns = await readDb.Campaigns
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
                ROI = c.Roi
            })
            .OrderByDescending(c => c.ROI)
            .ToListAsync();

        decimal totalBudget = campaigns.Sum(c => c.Budget);
        decimal totalExpenses = campaigns.Sum(c => c.TotalExpenses);
        decimal totalRevenue = campaigns.Sum(c => c.TotalRevenue);
        decimal overallRoi = totalExpenses > 0 ? (totalRevenue - totalExpenses) / totalExpenses * 100 : 0;

        return this.Ok(new { campaigns, totalBudget, totalExpenses, totalRevenue,
            overallROI = overallRoi });
    }

    #endregion

    #region Commands

    /// <summary>
    /// Create a new campaign
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest request)
    {
        string campaignNumber = $"CMP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        Campaign campaign = Campaign.Create(
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

        await repository.SaveAsync(campaign);

        return this.CreatedAtAction(nameof(this.GetCampaign), new { id = campaign.Id },
            new { id = campaign.Id, campaignNumber = campaign.CampaignNumber });
    }

    /// <summary>
    /// Start a campaign
    /// </summary>
    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> StartCampaign(Guid id)
    {
        Campaign? campaign = await repository.LoadAsync(id);
        if (campaign == null) return this.NotFound();

        campaign.Start();
        await repository.SaveAsync(campaign);

        return this.Ok(new { id, status = "Active" });
    }

    /// <summary>
    /// Pause a campaign
    /// </summary>
    [HttpPost("{id:guid}/pause")]
    public async Task<IActionResult> PauseCampaign(Guid id)
    {
        Campaign? campaign = await repository.LoadAsync(id);
        if (campaign == null) return this.NotFound();

        campaign.Pause();
        await repository.SaveAsync(campaign);

        return this.Ok(new { id, status = "Paused" });
    }

    /// <summary>
    /// Resume a campaign
    /// </summary>
    [HttpPost("{id:guid}/resume")]
    public async Task<IActionResult> ResumeCampaign(Guid id)
    {
        Campaign? campaign = await repository.LoadAsync(id);
        if (campaign == null) return this.NotFound();

        campaign.Resume();
        await repository.SaveAsync(campaign);

        return this.Ok(new { id, status = "Active" });
    }

    /// <summary>
    /// Complete a campaign
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> CompleteCampaign(Guid id)
    {
        Campaign? campaign = await repository.LoadAsync(id);
        if (campaign == null) return this.NotFound();

        campaign.Complete();
        await repository.SaveAsync(campaign);

        return this.Ok(new { id, status = "Completed" });
    }

    /// <summary>
    /// Cancel a campaign
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelCampaign(Guid id)
    {
        Campaign? campaign = await repository.LoadAsync(id);
        if (campaign == null) return this.NotFound();

        campaign.Cancel();
        await repository.SaveAsync(campaign);

        return this.Ok(new { id, status = "Cancelled" });
    }

    /// <summary>
    /// Schedule a campaign
    /// </summary>
    [HttpPost("{id:guid}/schedule")]
    public async Task<IActionResult> ScheduleCampaign(Guid id)
    {
        Campaign? campaign = await repository.LoadAsync(id);
        if (campaign == null) return this.NotFound();

        campaign.Schedule();
        await repository.SaveAsync(campaign);

        return this.Ok(new { id, status = "Scheduled" });
    }

    /// <summary>
    /// Update campaign budget
    /// </summary>
    [HttpPut("{id:guid}/budget")]
    public async Task<IActionResult> UpdateBudget(Guid id, [FromBody] UpdateBudgetRequest request)
    {
        Campaign? campaign = await repository.LoadAsync(id);
        if (campaign == null) return this.NotFound();

        campaign.UpdateBudget(request.Budget, request.Reason);
        await repository.SaveAsync(campaign);

        return this.Ok(new { id, budget = request.Budget });
    }

    /// <summary>
    /// Associate a lead with this campaign
    /// </summary>
    [HttpPost("{id:guid}/leads")]
    public async Task<IActionResult> AssociateLead(Guid id, [FromBody] AssociateLeadRequest request)
    {
        Campaign? campaign = await repository.LoadAsync(id);
        if (campaign == null) return this.NotFound();

        campaign.AssociateLead(request.LeadId, request.LeadNumber);
        await repository.SaveAsync(campaign);

        return this.Ok(new { id, message = "Lead associated" });
    }

    /// <summary>
    /// Record an expense for the campaign
    /// </summary>
    [HttpPost("{id:guid}/expenses")]
    public async Task<IActionResult> RecordExpense(Guid id, [FromBody] RecordExpenseRequest request)
    {
        Campaign? campaign = await repository.LoadAsync(id);
        if (campaign == null) return this.NotFound();

        campaign.RecordExpense(
            request.Description,
            request.Amount,
            request.ExpenseDate ?? DateTime.UtcNow,
            request.RecordedByUserId);

        await repository.SaveAsync(campaign);

        return this.Ok(new { id, message = "Expense recorded" });
    }

    /// <summary>
    /// Update campaign metrics
    /// </summary>
    [HttpPut("{id:guid}/metrics")]
    public async Task<IActionResult> UpdateMetrics(Guid id, [FromBody] UpdateMetricsRequest request)
    {
        Campaign? campaign = await repository.LoadAsync(id);
        if (campaign == null) return this.NotFound();

        campaign.UpdateMetrics(request.TotalLeads, request.ConvertedLeads, request.TotalRevenue);
        await repository.SaveAsync(campaign);

        return this.Ok(new { id, message = "Metrics updated" });
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
