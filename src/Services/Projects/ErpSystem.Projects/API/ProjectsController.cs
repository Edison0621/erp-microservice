using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Projects.Domain;
using ErpSystem.Projects.Infrastructure;

namespace ErpSystem.Projects.API;

[ApiController]
[Route("api/v1/projects/projects")]
public class ProjectsController(IEventStore eventStore, ProjectsReadDbContext readDb) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProjects(
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] string? managerId = null)
    {
        IQueryable<ProjectReadModel> query = readDb.Projects.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);
        if (!string.IsNullOrEmpty(type))
            query = query.Where(p => p.Type == type);
        if (!string.IsNullOrEmpty(managerId))
            query = query.Where(p => p.ProjectManagerId == managerId);

        List<ProjectReadModel> projects = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        return this.Ok(new { items = projects, total = projects.Count });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProject(Guid id)
    {
        ProjectReadModel? project = await readDb.Projects.FindAsync(id);
        return project == null ? this.NotFound() : this.Ok(project);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
    {
        string projectNumber = $"PRJ-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        
        Project project = Project.Create(
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

        await eventStore.SaveAggregateAsync(project);
        return this.CreatedAtAction(nameof(this.GetProject), new { id = project.Id }, new { id = project.Id, projectNumber });
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeStatusRequest request)
    {
        Project? project = await eventStore.LoadAggregateAsync<Project>(id);
        if (project == null) return this.NotFound();

        project.ChangeStatus(Enum.Parse<ProjectStatus>(request.Status));
        await eventStore.SaveAggregateAsync(project);
        return this.Ok(new { id, status = request.Status });
    }

    [HttpPost("{id:guid}/tasks")]
    public async Task<IActionResult> AddTask(Guid id, [FromBody] AddTaskRequest request)
    {
        Project? project = await eventStore.LoadAggregateAsync<Project>(id);
        if (project == null) return this.NotFound();

        Guid taskId = project.AddTask(
            request.Title,
            request.Description,
            Enum.Parse<TaskPriority>(request.Priority),
            request.DueDate,
            request.AssigneeId,
            request.EstimatedHours,
            request.ParentTaskId
        );

        await eventStore.SaveAggregateAsync(project);
        return this.Ok(new { taskId });
    }

    [HttpPost("{id:guid}/milestones")]
    public async Task<IActionResult> AddMilestone(Guid id, [FromBody] AddMilestoneRequest request)
    {
        Project? project = await eventStore.LoadAggregateAsync<Project>(id);
        if (project == null) return this.NotFound();

        Guid milestoneId = project.AddMilestone(request.Name, request.DueDate, request.Description);
        await eventStore.SaveAggregateAsync(project);
        return this.Ok(new { milestoneId });
    }

    [HttpPost("{id:guid}/team-members")]
    public async Task<IActionResult> AddTeamMember(Guid id, [FromBody] AddTeamMemberRequest request)
    {
        Project? project = await eventStore.LoadAggregateAsync<Project>(id);
        if (project == null) return this.NotFound();

        project.AddTeamMember(request.UserId, request.Role);
        await eventStore.SaveAggregateAsync(project);
        return this.Ok();
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        List<ProjectReadModel> projects = await readDb.Projects.ToListAsync();
        return this.Ok(new
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
