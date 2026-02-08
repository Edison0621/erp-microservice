using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Projects.Domain;
using ErpSystem.Projects.Infrastructure;

namespace ErpSystem.Projects.API;

[ApiController]
[Route("api/v1/projects/projects")]
public class ProjectsController : ControllerBase
{
    private readonly IEventStore _eventStore;
    private readonly ProjectsReadDbContext _readDb;

    public ProjectsController(IEventStore eventStore, ProjectsReadDbContext readDb)
    {
        _eventStore = eventStore;
        _readDb = readDb;
    }

    [HttpGet]
    public async Task<IActionResult> GetProjects(
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] string? managerId = null)
    {
        var query = _readDb.Projects.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);
        if (!string.IsNullOrEmpty(type))
            query = query.Where(p => p.Type == type);
        if (!string.IsNullOrEmpty(managerId))
            query = query.Where(p => p.ProjectManagerId == managerId);

        var projects = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        return Ok(new { items = projects, total = projects.Count });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProject(Guid id)
    {
        var project = await _readDb.Projects.FindAsync(id);
        return project == null ? NotFound() : Ok(project);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
    {
        var projectNumber = $"PRJ-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        
        var project = Project.Create(
            Guid.NewGuid(),
            projectNumber,
            request.Name,
            Enum.Parse<ProjectType>(request.Type),
            request.StartDate,
            request.EndDate,
            request.Budget,
            request.Currency,
            request.ProjectManagerId,
            request.CustomerId,
            request.Description
        );

        await _eventStore.SaveAggregateAsync(project);
        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, new { id = project.Id, projectNumber });
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeStatusRequest request)
    {
        var project = await _eventStore.LoadAggregateAsync<Project>(id);
        if (project == null) return NotFound();

        project.ChangeStatus(Enum.Parse<ProjectStatus>(request.Status));
        await _eventStore.SaveAggregateAsync(project);
        return Ok(new { id, status = request.Status });
    }

    [HttpPost("{id:guid}/tasks")]
    public async Task<IActionResult> AddTask(Guid id, [FromBody] AddTaskRequest request)
    {
        var project = await _eventStore.LoadAggregateAsync<Project>(id);
        if (project == null) return NotFound();

        var taskId = project.AddTask(
            request.Title,
            request.Description,
            Enum.Parse<TaskPriority>(request.Priority),
            request.DueDate,
            request.AssigneeId,
            request.EstimatedHours,
            request.ParentTaskId
        );

        await _eventStore.SaveAggregateAsync(project);
        return Ok(new { taskId });
    }

    [HttpPost("{id:guid}/milestones")]
    public async Task<IActionResult> AddMilestone(Guid id, [FromBody] AddMilestoneRequest request)
    {
        var project = await _eventStore.LoadAggregateAsync<Project>(id);
        if (project == null) return NotFound();

        var milestoneId = project.AddMilestone(request.Name, request.DueDate, request.Description);
        await _eventStore.SaveAggregateAsync(project);
        return Ok(new { milestoneId });
    }

    [HttpPost("{id:guid}/team-members")]
    public async Task<IActionResult> AddTeamMember(Guid id, [FromBody] AddTeamMemberRequest request)
    {
        var project = await _eventStore.LoadAggregateAsync<Project>(id);
        if (project == null) return NotFound();

        project.AddTeamMember(request.UserId, request.Role);
        await _eventStore.SaveAggregateAsync(project);
        return Ok();
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var projects = await _readDb.Projects.ToListAsync();
        return Ok(new
        {
            total = projects.Count,
            planning = projects.Count(p => p.Status == "Planning"),
            inProgress = projects.Count(p => p.Status == "InProgress"),
            completed = projects.Count(p => p.Status == "Completed"),
            onHold = projects.Count(p => p.Status == "OnHold"),
            totalBudget = projects.Sum(p => p.PlannedBudget),
            avgProgress = projects.Any() ? projects.Average(p => p.ProgressPercent) : 0
        });
    }
}

#region Request DTOs

public record CreateProjectRequest(
    string Name,
    string Type,
    DateTime StartDate,
    DateTime EndDate,
    decimal Budget,
    string Currency,
    string ProjectManagerId,
    string? CustomerId,
    string? Description
);

public record ChangeStatusRequest(string Status);

public record AddTaskRequest(
    string Title,
    string? Description,
    string Priority,
    DateTime? DueDate,
    string? AssigneeId,
    int EstimatedHours,
    Guid? ParentTaskId
);

public record AddMilestoneRequest(string Name, DateTime DueDate, string? Description);

public record AddTeamMemberRequest(string UserId, string Role);

#endregion
