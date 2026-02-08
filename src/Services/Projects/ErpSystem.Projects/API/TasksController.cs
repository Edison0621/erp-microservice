using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Projects.Domain;
using ErpSystem.Projects.Infrastructure;

namespace ErpSystem.Projects.API;

[ApiController]
[Route("api/v1/projects/tasks")]
public class TasksController : ControllerBase
{
    private readonly IEventStore _eventStore;
    private readonly ProjectsReadDbContext _readDb;

    public TasksController(IEventStore eventStore, ProjectsReadDbContext readDb)
    {
        _eventStore = eventStore;
        _readDb = readDb;
    }

    [HttpGet]
    public async Task<IActionResult> GetTasks(
        [FromQuery] Guid? projectId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? assigneeId = null)
    {
        var query = _readDb.Tasks.AsQueryable();

        if (projectId.HasValue)
            query = query.Where(t => t.ProjectId == projectId.Value);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);
        if (!string.IsNullOrEmpty(assigneeId))
            query = query.Where(t => t.AssigneeId == assigneeId);

        var tasks = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return Ok(new { items = tasks, total = tasks.Count });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTask(Guid id)
    {
        var task = await _readDb.Tasks.FindAsync(id);
        return task == null ? NotFound() : Ok(task);
    }

    [HttpPut("{id:guid}/progress")]
    public async Task<IActionResult> UpdateProgress(Guid id, [FromBody] UpdateProgressRequest request)
    {
        var task = await _readDb.Tasks.FindAsync(id);
        if (task == null) return NotFound();

        var project = await _eventStore.LoadAggregateAsync<Project>(task.ProjectId);
        if (project == null) return NotFound();

        project.UpdateTaskProgress(id, request.ProgressPercent);
        await _eventStore.SaveAggregateAsync(project);
        return Ok(new { id, progress = request.ProgressPercent });
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> CompleteTask(Guid id, [FromBody] CompleteTaskRequest request)
    {
        var task = await _readDb.Tasks.FindAsync(id);
        if (task == null) return NotFound();

        var project = await _eventStore.LoadAggregateAsync<Project>(task.ProjectId);
        if (project == null) return NotFound();

        project.CompleteTask(id, request.ActualHours);
        await _eventStore.SaveAggregateAsync(project);
        return Ok(new { id, completed = true });
    }

    [HttpGet("kanban/{projectId:guid}")]
    public async Task<IActionResult> GetKanbanBoard(Guid projectId)
    {
        var tasks = await _readDb.Tasks.Where(t => t.ProjectId == projectId).ToListAsync();
        
        return Ok(new
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
        var tasks = await _readDb.Tasks
            .Where(t => t.AssigneeId == userId && t.Status != "Completed")
            .OrderBy(t => t.DueDate)
            .ToListAsync();
        return Ok(new { items = tasks, total = tasks.Count });
    }
}

#region Request DTOs

public record UpdateProgressRequest(int ProgressPercent);
public record CompleteTaskRequest(int ActualHours);

#endregion
