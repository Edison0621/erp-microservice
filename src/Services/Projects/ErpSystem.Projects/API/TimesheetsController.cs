using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Projects.Domain;
using ErpSystem.Projects.Infrastructure;

namespace ErpSystem.Projects.API;

[ApiController]
[Route("api/v1/projects/timesheets")]
public class TimesheetsController : ControllerBase
{
    private readonly IEventStore _eventStore;
    private readonly ProjectsReadDbContext _readDb;

    public TimesheetsController(IEventStore eventStore, ProjectsReadDbContext readDb)
    {
        _eventStore = eventStore;
        _readDb = readDb;
    }

    [HttpGet]
    public async Task<IActionResult> GetTimesheets(
        [FromQuery] Guid? projectId = null,
        [FromQuery] string? userId = null,
        [FromQuery] string? status = null)
    {
        var query = _readDb.Timesheets.AsQueryable();

        if (projectId.HasValue)
            query = query.Where(t => t.ProjectId == projectId.Value);
        if (!string.IsNullOrEmpty(userId))
            query = query.Where(t => t.UserId == userId);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);

        var timesheets = await query.OrderByDescending(t => t.WeekStartDate).ToListAsync();
        return Ok(new { items = timesheets, total = timesheets.Count });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTimesheet(Guid id)
    {
        var timesheet = await _readDb.Timesheets.FindAsync(id);
        return timesheet == null ? NotFound() : Ok(timesheet);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTimesheet([FromBody] CreateTimesheetRequest request)
    {
        var timesheetNumber = $"TS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

        var timesheet = Timesheet.Create(
            Guid.NewGuid(),
            timesheetNumber,
            request.ProjectId,
            request.UserId,
            request.WeekStartDate
        );

        await _eventStore.SaveAggregateAsync(timesheet);
        return CreatedAtAction(nameof(GetTimesheet), new { id = timesheet.Id }, new { id = timesheet.Id, timesheetNumber });
    }

    [HttpPost("{id:guid}/entries")]
    public async Task<IActionResult> AddEntry(Guid id, [FromBody] AddEntryRequest request)
    {
        var timesheet = await _eventStore.LoadAggregateAsync<Timesheet>(id);
        if (timesheet == null) return NotFound();

        timesheet.AddEntry(request.TaskId, request.WorkDate, request.Hours, request.Description);
        await _eventStore.SaveAggregateAsync(timesheet);
        return Ok(new { id, totalHours = timesheet.TotalHours });
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id)
    {
        var timesheet = await _eventStore.LoadAggregateAsync<Timesheet>(id);
        if (timesheet == null) return NotFound();

        timesheet.Submit();
        await _eventStore.SaveAggregateAsync(timesheet);
        return Ok(new { id, status = "Submitted" });
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveRequest request)
    {
        var timesheet = await _eventStore.LoadAggregateAsync<Timesheet>(id);
        if (timesheet == null) return NotFound();

        timesheet.Approve(request.ApprovedByUserId);
        await _eventStore.SaveAggregateAsync(timesheet);
        return Ok(new { id, status = "Approved" });
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectRequest request)
    {
        var timesheet = await _eventStore.LoadAggregateAsync<Timesheet>(id);
        if (timesheet == null) return NotFound();

        timesheet.Reject(request.RejectedByUserId, request.Reason);
        await _eventStore.SaveAggregateAsync(timesheet);
        return Ok(new { id, status = "Rejected" });
    }

    [HttpGet("pending-approval")]
    public async Task<IActionResult> GetPendingApproval()
    {
        var timesheets = await _readDb.Timesheets
            .Where(t => t.Status == "Submitted")
            .OrderBy(t => t.SubmittedAt)
            .ToListAsync();
        return Ok(new { items = timesheets, total = timesheets.Count });
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] Guid projectId)
    {
        var timesheets = await _readDb.Timesheets
            .Where(t => t.ProjectId == projectId && t.Status == "Approved")
            .ToListAsync();

        return Ok(new
        {
            totalHours = timesheets.Sum(t => t.TotalHours),
            totalTimesheets = timesheets.Count,
            byUser = timesheets.GroupBy(t => t.UserId).Select(g => new
            {
                userId = g.Key,
                totalHours = g.Sum(t => t.TotalHours)
            })
        });
    }
}

#region Request DTOs

public record CreateTimesheetRequest(Guid ProjectId, string UserId, DateTime WeekStartDate);
public record AddEntryRequest(Guid TaskId, DateTime WorkDate, decimal Hours, string? Description);
public record ApproveRequest(string ApprovedByUserId);
public record RejectRequest(string RejectedByUserId, string Reason);

#endregion
