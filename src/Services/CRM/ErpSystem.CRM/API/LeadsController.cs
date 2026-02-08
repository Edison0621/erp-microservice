using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.CRM.Domain;
using ErpSystem.CRM.Infrastructure;

namespace ErpSystem.CRM.API;

[ApiController]
[Route("api/[controller]")]
public class LeadsController : ControllerBase
{
    private readonly EventStoreRepository<Lead> _repository;
    private readonly CrmReadDbContext _readDb;

    public LeadsController(EventStoreRepository<Lead> repository, CrmReadDbContext readDb)
    {
        _repository = repository;
        _readDb = readDb;
    }

    #region Queries

    /// <summary>
    /// Get all leads with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLeads(
        [FromQuery] string? status = null,
        [FromQuery] string? source = null,
        [FromQuery] string? assignedTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _readDb.Leads.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(l => l.Status == status);
        if (!string.IsNullOrEmpty(source))
            query = query.Where(l => l.Source == source);
        if (!string.IsNullOrEmpty(assignedTo))
            query = query.Where(l => l.AssignedToUserId == assignedTo);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    /// <summary>
    /// Get lead by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetLead(Guid id)
    {
        var lead = await _readDb.Leads.FindAsync(id);
        if (lead == null) return NotFound();
        return Ok(lead);
    }

    /// <summary>
    /// Get lead statistics by status
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetLeadStatistics()
    {
        var stats = await _readDb.Leads
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var totalScore = await _readDb.Leads.AverageAsync(l => (double?)l.Score) ?? 0;

        return Ok(new { byStatus = stats, averageScore = totalScore });
    }

    #endregion

    #region Commands

    /// <summary>
    /// Create a new lead
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateLead([FromBody] CreateLeadRequest request)
    {
        var leadNumber = $"LD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        
        var contact = new ContactInfo(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone ?? "",
            request.Mobile ?? "");

        CompanyInfo? company = null;
        if (!string.IsNullOrEmpty(request.CompanyName))
        {
            company = new CompanyInfo(
                request.CompanyName,
                request.Industry ?? "",
                request.CompanySize ?? "",
                request.Website ?? "",
                request.Address ?? "");
        }

        var lead = Lead.Create(
            Guid.NewGuid(),
            leadNumber,
            contact,
            company,
            Enum.Parse<LeadSource>(request.Source ?? "Website"),
            request.SourceDetails,
            request.AssignedToUserId,
            request.Notes);

        await _repository.SaveAsync(lead);

        return CreatedAtAction(nameof(GetLead), new { id = lead.Id }, 
            new { id = lead.Id, leadNumber = lead.LeadNumber });
    }

    /// <summary>
    /// Update lead status
    /// </summary>
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateLeadStatusRequest request)
    {
        var lead = await _repository.LoadAsync(id);
        if (lead == null) return NotFound();

        lead.ChangeStatus(Enum.Parse<LeadStatus>(request.Status), request.Reason);
        await _repository.SaveAsync(lead);

        return Ok(new { id, status = request.Status });
    }

    /// <summary>
    /// Qualify a lead with a score
    /// </summary>
    [HttpPost("{id:guid}/qualify")]
    public async Task<IActionResult> QualifyLead(Guid id, [FromBody] QualifyLeadRequest request)
    {
        var lead = await _repository.LoadAsync(id);
        if (lead == null) return NotFound();

        lead.Qualify(request.Score, request.Notes ?? "");
        await _repository.SaveAsync(lead);

        return Ok(new { id, score = request.Score, status = lead.Status.ToString() });
    }

    /// <summary>
    /// Convert lead to opportunity
    /// </summary>
    [HttpPost("{id:guid}/convert")]
    public async Task<IActionResult> ConvertToOpportunity(Guid id, [FromBody] ConvertLeadRequest request)
    {
        var lead = await _repository.LoadAsync(id);
        if (lead == null) return NotFound();

        var opportunityId = lead.ConvertToOpportunity(request.OpportunityName, request.EstimatedValue);
        await _repository.SaveAsync(lead);

        return Ok(new { id, opportunityId, opportunityName = request.OpportunityName });
    }

    /// <summary>
    /// Assign lead to user
    /// </summary>
    [HttpPut("{id:guid}/assign")]
    public async Task<IActionResult> AssignLead(Guid id, [FromBody] AssignLeadRequest request)
    {
        var lead = await _repository.LoadAsync(id);
        if (lead == null) return NotFound();

        lead.AssignTo(request.UserId);
        await _repository.SaveAsync(lead);

        return Ok(new { id, assignedTo = request.UserId });
    }

    /// <summary>
    /// Log a communication with the lead
    /// </summary>
    [HttpPost("{id:guid}/communications")]
    public async Task<IActionResult> LogCommunication(Guid id, [FromBody] LogCommunicationRequest request)
    {
        var lead = await _repository.LoadAsync(id);
        if (lead == null) return NotFound();

        lead.LogCommunication(
            Enum.Parse<CommunicationType>(request.Type),
            request.Subject,
            request.Content,
            request.CommunicationDate ?? DateTime.UtcNow,
            request.LoggedByUserId);

        await _repository.SaveAsync(lead);

        return Ok(new { id, message = "Communication logged" });
    }

    #endregion
}

#region Request DTOs

public record CreateLeadRequest(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? Mobile,
    string? CompanyName,
    string? Industry,
    string? CompanySize,
    string? Website,
    string? Address,
    string? Source,
    string? SourceDetails,
    string? AssignedToUserId,
    string? Notes
);

public record UpdateLeadStatusRequest(string Status, string? Reason);
public record QualifyLeadRequest(int Score, string? Notes);
public record ConvertLeadRequest(string OpportunityName, decimal EstimatedValue);
public record AssignLeadRequest(string UserId);

public record LogCommunicationRequest(
    string Type,
    string Subject,
    string Content,
    DateTime? CommunicationDate,
    string LoggedByUserId
);

#endregion
