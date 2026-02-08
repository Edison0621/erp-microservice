using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ErpSystem.CRM.Domain;

namespace ErpSystem.CRM.Infrastructure;

#region Lead Projections

public class LeadProjectionHandler :
    INotificationHandler<LeadCreatedEvent>,
    INotificationHandler<LeadStatusChangedEvent>,
    INotificationHandler<LeadQualifiedEvent>,
    INotificationHandler<LeadConvertedToOpportunityEvent>,
    INotificationHandler<LeadAssignedEvent>,
    INotificationHandler<CommunicationLoggedEvent>
{
    private readonly CrmReadDbContext _db;

    public LeadProjectionHandler(CrmReadDbContext db) => _db = db;

    public async Task Handle(LeadCreatedEvent e, CancellationToken ct)
    {
        var lead = new LeadReadModel
        {
            Id = e.LeadId,
            LeadNumber = e.LeadNumber,
            Contact = JsonSerializer.Serialize(e.Contact),
            Company = e.Company != null ? JsonSerializer.Serialize(e.Company) : null,
            Status = LeadStatus.New.ToString(),
            Source = e.Source.ToString(),
            SourceDetails = e.SourceDetails,
            AssignedToUserId = e.AssignedToUserId,
            Notes = e.Notes,
            Score = 0,
            Communications = "[]",
            CreatedAt = e.OccurredOn
        };
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync(ct);
    }

    public async Task Handle(LeadStatusChangedEvent e, CancellationToken ct)
    {
        var lead = await _db.Leads.FindAsync([e.LeadId], ct);
        if (lead != null)
        {
            lead.Status = e.NewStatus.ToString();
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(LeadQualifiedEvent e, CancellationToken ct)
    {
        var lead = await _db.Leads.FindAsync([e.LeadId], ct);
        if (lead != null)
        {
            lead.Score = e.Score;
            if (e.Score >= 70)
                lead.Status = LeadStatus.Qualified.ToString();
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(LeadConvertedToOpportunityEvent e, CancellationToken ct)
    {
        var lead = await _db.Leads.FindAsync([e.LeadId], ct);
        if (lead != null)
        {
            lead.Status = LeadStatus.Converted.ToString();
            lead.ConvertedOpportunityId = e.OpportunityId;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(LeadAssignedEvent e, CancellationToken ct)
    {
        var lead = await _db.Leads.FindAsync([e.LeadId], ct);
        if (lead != null)
        {
            lead.AssignedToUserId = e.NewUserId;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(CommunicationLoggedEvent e, CancellationToken ct)
    {
        var lead = await _db.Leads.FindAsync([e.LeadId], ct);
        if (lead != null)
        {
            var communications = JsonSerializer.Deserialize<List<CommunicationRecord>>(lead.Communications) ?? new();
            communications.Add(new CommunicationRecord(
                e.CommunicationId, e.Type, e.Subject, e.Content, e.CommunicationDate, e.LoggedByUserId));
            lead.Communications = JsonSerializer.Serialize(communications);
            lead.LastContactedAt = e.CommunicationDate;
            if (lead.Status == LeadStatus.New.ToString())
                lead.Status = LeadStatus.Contacted.ToString();
            await _db.SaveChangesAsync(ct);
        }
    }
}

#endregion

#region Opportunity Projections

public class OpportunityProjectionHandler :
    INotificationHandler<OpportunityCreatedEvent>,
    INotificationHandler<OpportunityStageChangedEvent>,
    INotificationHandler<OpportunityValueUpdatedEvent>,
    INotificationHandler<OpportunityWonEvent>,
    INotificationHandler<OpportunityLostEvent>,
    INotificationHandler<OpportunityAssignedEvent>,
    INotificationHandler<OpportunityCompetitorAddedEvent>,
    INotificationHandler<OpportunityActivityLoggedEvent>
{
    private readonly CrmReadDbContext _db;

    public OpportunityProjectionHandler(CrmReadDbContext db) => _db = db;

    public async Task Handle(OpportunityCreatedEvent e, CancellationToken ct)
    {
        var opp = new OpportunityReadModel
        {
            Id = e.OpportunityId,
            OpportunityNumber = e.OpportunityNumber,
            Name = e.Name,
            LeadId = e.LeadId,
            CustomerId = e.CustomerId,
            CustomerName = e.CustomerName,
            EstimatedValue = e.EstimatedValue,
            WeightedValue = e.EstimatedValue * 0.1m, // 10% for Prospecting
            Currency = e.Currency,
            ExpectedCloseDate = e.ExpectedCloseDate,
            Stage = OpportunityStage.Prospecting.ToString(),
            Priority = e.Priority.ToString(),
            WinProbability = 10,
            AssignedToUserId = e.AssignedToUserId,
            Description = e.Description,
            Competitors = "[]",
            Activities = "[]",
            CreatedAt = e.OccurredOn
        };
        _db.Opportunities.Add(opp);
        await _db.SaveChangesAsync(ct);
    }

    public async Task Handle(OpportunityStageChangedEvent e, CancellationToken ct)
    {
        var opp = await _db.Opportunities.FindAsync([e.OpportunityId], ct);
        if (opp != null)
        {
            opp.Stage = e.NewStage.ToString();
            opp.WinProbability = e.WinProbability;
            opp.WeightedValue = opp.EstimatedValue * e.WinProbability / 100m;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(OpportunityValueUpdatedEvent e, CancellationToken ct)
    {
        var opp = await _db.Opportunities.FindAsync([e.OpportunityId], ct);
        if (opp != null)
        {
            opp.EstimatedValue = e.NewValue;
            opp.WeightedValue = e.NewValue * opp.WinProbability / 100m;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(OpportunityWonEvent e, CancellationToken ct)
    {
        var opp = await _db.Opportunities.FindAsync([e.OpportunityId], ct);
        if (opp != null)
        {
            opp.Stage = OpportunityStage.ClosedWon.ToString();
            opp.WinProbability = 100;
            opp.EstimatedValue = e.FinalValue;
            opp.WeightedValue = e.FinalValue;
            opp.WinReason = e.WinReason;
            opp.SalesOrderId = e.SalesOrderId;
            opp.ClosedAt = e.OccurredOn;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(OpportunityLostEvent e, CancellationToken ct)
    {
        var opp = await _db.Opportunities.FindAsync([e.OpportunityId], ct);
        if (opp != null)
        {
            opp.Stage = OpportunityStage.ClosedLost.ToString();
            opp.WinProbability = 0;
            opp.WeightedValue = 0;
            opp.LossReason = e.LossReason;
            opp.ClosedAt = e.OccurredOn;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(OpportunityAssignedEvent e, CancellationToken ct)
    {
        var opp = await _db.Opportunities.FindAsync([e.OpportunityId], ct);
        if (opp != null)
        {
            opp.AssignedToUserId = e.NewUserId;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(OpportunityCompetitorAddedEvent e, CancellationToken ct)
    {
        var opp = await _db.Opportunities.FindAsync([e.OpportunityId], ct);
        if (opp != null)
        {
            var competitors = JsonSerializer.Deserialize<List<CompetitorInfo>>(opp.Competitors) ?? new();
            competitors.Add(new CompetitorInfo(e.CompetitorId, e.CompetitorName, e.Strengths, e.Weaknesses));
            opp.Competitors = JsonSerializer.Serialize(competitors);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(OpportunityActivityLoggedEvent e, CancellationToken ct)
    {
        var opp = await _db.Opportunities.FindAsync([e.OpportunityId], ct);
        if (opp != null)
        {
            var activities = JsonSerializer.Deserialize<List<ActivityRecord>>(opp.Activities) ?? new();
            activities.Add(new ActivityRecord(
                e.ActivityId, e.ActivityType, e.Subject, e.Description, e.ActivityDate, e.LoggedByUserId));
            opp.Activities = JsonSerializer.Serialize(activities);
            await _db.SaveChangesAsync(ct);
        }
    }
}

#endregion

#region Campaign Projections

public class CampaignProjectionHandler :
    INotificationHandler<CampaignCreatedEvent>,
    INotificationHandler<CampaignStatusChangedEvent>,
    INotificationHandler<CampaignBudgetUpdatedEvent>,
    INotificationHandler<LeadAssociatedToCampaignEvent>,
    INotificationHandler<CampaignExpenseRecordedEvent>,
    INotificationHandler<CampaignMetricsUpdatedEvent>
{
    private readonly CrmReadDbContext _db;

    public CampaignProjectionHandler(CrmReadDbContext db) => _db = db;

    public async Task Handle(CampaignCreatedEvent e, CancellationToken ct)
    {
        var campaign = new CampaignReadModel
        {
            Id = e.CampaignId,
            CampaignNumber = e.CampaignNumber,
            Name = e.Name,
            Type = e.Type.ToString(),
            Status = CampaignStatus.Draft.ToString(),
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            Budget = e.Budget,
            Currency = e.Currency,
            TargetAudience = e.TargetAudience,
            Description = e.Description,
            CreatedByUserId = e.CreatedByUserId,
            AssociatedLeads = "[]",
            Expenses = "[]",
            CreatedAt = e.OccurredOn
        };
        _db.Campaigns.Add(campaign);
        await _db.SaveChangesAsync(ct);
    }

    public async Task Handle(CampaignStatusChangedEvent e, CancellationToken ct)
    {
        var campaign = await _db.Campaigns.FindAsync([e.CampaignId], ct);
        if (campaign != null)
        {
            campaign.Status = e.NewStatus.ToString();
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(CampaignBudgetUpdatedEvent e, CancellationToken ct)
    {
        var campaign = await _db.Campaigns.FindAsync([e.CampaignId], ct);
        if (campaign != null)
        {
            campaign.Budget = e.NewBudget;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(LeadAssociatedToCampaignEvent e, CancellationToken ct)
    {
        var campaign = await _db.Campaigns.FindAsync([e.CampaignId], ct);
        if (campaign != null)
        {
            var leads = JsonSerializer.Deserialize<List<CampaignLead>>(campaign.AssociatedLeads) ?? new();
            leads.Add(new CampaignLead(e.LeadId, e.LeadNumber, e.AssociatedAt));
            campaign.AssociatedLeads = JsonSerializer.Serialize(leads);
            campaign.TotalLeads = leads.Count;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(CampaignExpenseRecordedEvent e, CancellationToken ct)
    {
        var campaign = await _db.Campaigns.FindAsync([e.CampaignId], ct);
        if (campaign != null)
        {
            var expenses = JsonSerializer.Deserialize<List<CampaignExpense>>(campaign.Expenses) ?? new();
            expenses.Add(new CampaignExpense(e.ExpenseId, e.Description, e.Amount, e.ExpenseDate, e.RecordedByUserId));
            campaign.Expenses = JsonSerializer.Serialize(expenses);
            campaign.TotalExpenses = expenses.Sum(exp => exp.Amount);
            
            // Recalculate metrics
            if (campaign.TotalLeads > 0)
                campaign.CostPerLead = campaign.TotalExpenses / campaign.TotalLeads;
            if (campaign.TotalExpenses > 0)
                campaign.ROI = (campaign.TotalRevenue - campaign.TotalExpenses) / campaign.TotalExpenses * 100;
                
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(CampaignMetricsUpdatedEvent e, CancellationToken ct)
    {
        var campaign = await _db.Campaigns.FindAsync([e.CampaignId], ct);
        if (campaign != null)
        {
            campaign.TotalLeads = e.TotalLeads;
            campaign.ConvertedLeads = e.ConvertedLeads;
            campaign.TotalRevenue = e.TotalRevenue;
            
            // Recalculate derived metrics
            if (e.TotalLeads > 0)
            {
                campaign.CostPerLead = campaign.TotalExpenses / e.TotalLeads;
                campaign.ConversionRate = (decimal)e.ConvertedLeads / e.TotalLeads * 100;
            }
            if (campaign.TotalExpenses > 0)
                campaign.ROI = (e.TotalRevenue - campaign.TotalExpenses) / campaign.TotalExpenses * 100;
                
            await _db.SaveChangesAsync(ct);
        }
    }
}

#endregion
