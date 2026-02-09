using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Projects.Domain;
using ErpSystem.Projects.Infrastructure;

namespace ErpSystem.Projects.API;

[ApiController]
[Route("api/v1/projects/tasks")]
public class TasksController(IEventStore eventStore, ProjectsReadDbContext readDb) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTasks(
        [FromQuery] Guid? projectId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? assigneeId = null)
    {
        IQueryable<TaskReadModel> query = readDb.Tasks.AsQueryable();

        if (projectId.HasValue)
            query = query.Where(t => t.ProjectId == projectId.Value);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);
        if (!string.IsNullOrEmpty(assigneeId))
            query = query.Where(t => t.AssigneeId == assigneeId);

        List<TaskReadModel> tasks = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return this.Ok(new { items = tasks, total = tasks.Count });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTask(Guid id)
    {
        TaskReadModel? task = await readDb.Tasks.FindAsync(id);
        return task == null ? this.NotFound() : this.Ok(task);
    }

    [HttpPut("{id:guid}/progress")]
    public async Task<IActionResult> UpdateProgress(Guid id, [FromBody] UpdateProgressRequest request)
    {
        TaskReadModel? task = await readDb.Tasks.FindAsync(id);
        if (task == null) return this.NotFound();

        Project? project = await eventStore.LoadAggregateAsync<Project>(task.ProjectId);
        if (project == null) return this.NotFound();

        project.UpdateTaskProgress(id, request.ProgressPercent);
        await eventStore.SaveAggregateAsync(project);
        return this.Ok(new { id, progress = request.ProgressPercent });
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> CompleteTask(Guid id, [FromBody] CompleteTaskRequest request)
    {
        TaskReadModel? task = await readDb.Tasks.FindAsync(id);
        if (task == null) return this.NotFound();

        Project? project = await eventStore.LoadAggregateAsync<Project>(task.ProjectId);
        if (project == null) return this.NotFound();

        project.CompleteTask(id, request.ActualHours);
        await eventStore.SaveAggregateAsync(project);
        return this.Ok(new { id, completed = true });
    }

    [HttpGet("kanban/{projectId:guid}")]
    public async Task<IActionResult> GetKanbanBoard(Guid projectId)
    {
        List<TaskReadModel> tasks = await readDb.Tasks.Where(t => t.ProjectId == projectId).ToListAsync();
        
        return this.Ok(new
        {
            columns = new[]
            {
                new { status = "Open", tasks = tasks.Where(t => t.Status == "Open").ToList() },
                new { status = "InProgress", tasks = tasks.Where(t => t.Status == "InProgress").ToList() },
                new { status = "InReview", tasks = tasks.Where(t => t.Status == "InReview").ToList() },
                new { status = "Completed", tasks = tasks.Where(t => t.Status == "Completed").ToList() }
            }
        });
    }

    [HttpGet("my-tasks")]
    public async Task<IActionResult> GetMyTasks([FromQuery] string userId)
    {
        List<TaskReadModel> tasks = await readDb.Tasks
            .Where(t => t.AssigneeId == userId && t.Status != "Completed")
            .OrderBy(t => t.DueDate)
            .ToListAsync();
        return this.Ok(new { items = tasks, total = tasks.Count });
    }
}

#region Request DTOs

public record UpdateProgressRequest(int ProgressPercent);

public record CompleteTaskRequest(int ActualHours);

#endregion
