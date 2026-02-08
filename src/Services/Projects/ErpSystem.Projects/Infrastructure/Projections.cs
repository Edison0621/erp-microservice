using MediatR;
using System.Text.Json;
using ErpSystem.Projects.Domain;

namespace ErpSystem.Projects.Infrastructure;

#region Project Projections

public class ProjectProjectionHandler :
    INotificationHandler<ProjectCreatedEvent>,
    INotificationHandler<ProjectStatusChangedEvent>,
    INotificationHandler<TaskAddedEvent>,
    INotificationHandler<TaskCompletedEvent>,
    INotificationHandler<MilestoneAddedEvent>,
    INotificationHandler<MilestoneReachedEvent>,
    INotificationHandler<TeamMemberAddedEvent>,
    INotificationHandler<ProjectBudgetUpdatedEvent>
{
    private readonly ProjectsReadDbContext _db;

    public ProjectProjectionHandler(ProjectsReadDbContext db) => _db = db;

    public async Task Handle(ProjectCreatedEvent e, CancellationToken ct)
    {
        var project = new ProjectReadModel
        {
            Id = e.ProjectId,
            ProjectNumber = e.ProjectNumber,
            Name = e.Name,
            Description = e.Description,
            Type = e.Type.ToString(),
            Status = ProjectStatus.Planning.ToString(),
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            PlannedBudget = e.Budget,
            Currency = e.Currency,
            CustomerId = e.CustomerId,
            ProjectManagerId = e.ProjectManagerId,
            Milestones = "[]",
            TeamMembers = "[]",
            CreatedAt = e.OccurredOn
        };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);
    }

    public async Task Handle(ProjectStatusChangedEvent e, CancellationToken ct)
    {
        var project = await _db.Projects.FindAsync([e.ProjectId], ct);
        if (project != null)
        {
            project.Status = e.NewStatus.ToString();
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(TaskAddedEvent e, CancellationToken ct)
    {
        var task = new TaskReadModel
        {
            Id = e.TaskId,
            ProjectId = e.ProjectId,
            TaskNumber = e.TaskNumber,
            Title = e.Title,
            Description = e.Description,
            Status = ProjectTaskStatus.Open.ToString(),
            Priority = e.Priority.ToString(),
            DueDate = e.DueDate,
            AssigneeId = e.AssigneeId,
            EstimatedHours = e.EstimatedHours,
            CreatedAt = DateTime.UtcNow
        };
        _db.Tasks.Add(task);

        var project = await _db.Projects.FindAsync([e.ProjectId], ct);
        if (project != null)
        {
            project.TotalTasks++;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task Handle(TaskCompletedEvent e, CancellationToken ct)
    {
        var task = await _db.Tasks.FindAsync([e.TaskId], ct);
        if (task != null)
        {
            task.Status = ProjectTaskStatus.Completed.ToString();
            task.ProgressPercent = 100;
            task.ActualHours = e.ActualHours;
            task.CompletedAt = e.CompletedAt;
        }

        var project = await _db.Projects.FindAsync([e.ProjectId], ct);
        if (project != null)
        {
            project.CompletedTasks++;
            project.ProgressPercent = project.TotalTasks > 0 
                ? (decimal)project.CompletedTasks / project.TotalTasks * 100 : 0;
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task Handle(MilestoneAddedEvent e, CancellationToken ct)
    {
        var project = await _db.Projects.FindAsync([e.ProjectId], ct);
        if (project != null)
        {
            var milestones = JsonSerializer.Deserialize<List<object>>(project.Milestones) ?? new();
            milestones.Add(new { e.MilestoneId, e.Name, e.DueDate, e.Description, IsReached = false });
            project.Milestones = JsonSerializer.Serialize(milestones);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(MilestoneReachedEvent e, CancellationToken ct)
    {
        // Update milestone in JSON - simplified for now
        await Task.CompletedTask;
    }

    public async Task Handle(TeamMemberAddedEvent e, CancellationToken ct)
    {
        var project = await _db.Projects.FindAsync([e.ProjectId], ct);
        if (project != null)
        {
            var members = JsonSerializer.Deserialize<List<TeamMember>>(project.TeamMembers) ?? new();
            members.Add(new TeamMember(e.UserId, e.Role, DateTime.UtcNow));
            project.TeamMembers = JsonSerializer.Serialize(members);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(ProjectBudgetUpdatedEvent e, CancellationToken ct)
    {
        var project = await _db.Projects.FindAsync([e.ProjectId], ct);
        if (project != null)
        {
            project.PlannedBudget = e.NewBudget;
            await _db.SaveChangesAsync(ct);
        }
    }
}

#endregion

#region Timesheet Projections

public class TimesheetProjectionHandler :
    INotificationHandler<TimesheetCreatedEvent>,
    INotificationHandler<TimesheetEntryAddedEvent>,
    INotificationHandler<TimesheetSubmittedEvent>,
    INotificationHandler<TimesheetApprovedEvent>,
    INotificationHandler<TimesheetRejectedEvent>
{
    private readonly ProjectsReadDbContext _db;

    public TimesheetProjectionHandler(ProjectsReadDbContext db) => _db = db;

    public async Task Handle(TimesheetCreatedEvent e, CancellationToken ct)
    {
        var timesheet = new TimesheetReadModel
        {
            Id = e.TimesheetId,
            TimesheetNumber = e.TimesheetNumber,
            ProjectId = e.ProjectId,
            UserId = e.UserId,
            WeekStartDate = e.WeekStartDate,
            WeekEndDate = e.WeekEndDate,
            Status = TimesheetStatus.Draft.ToString(),
            Entries = "[]"
        };
        _db.Timesheets.Add(timesheet);
        await _db.SaveChangesAsync(ct);
    }

    public async Task Handle(TimesheetEntryAddedEvent e, CancellationToken ct)
    {
        var timesheet = await _db.Timesheets.FindAsync([e.TimesheetId], ct);
        if (timesheet != null)
        {
            var entries = JsonSerializer.Deserialize<List<object>>(timesheet.Entries) ?? new();
            entries.Add(new { e.EntryId, e.TaskId, e.WorkDate, e.Hours, e.Description });
            timesheet.Entries = JsonSerializer.Serialize(entries);
            timesheet.TotalHours += e.Hours;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(TimesheetSubmittedEvent e, CancellationToken ct)
    {
        var timesheet = await _db.Timesheets.FindAsync([e.TimesheetId], ct);
        if (timesheet != null)
        {
            timesheet.Status = TimesheetStatus.Submitted.ToString();
            timesheet.SubmittedAt = e.SubmittedAt;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(TimesheetApprovedEvent e, CancellationToken ct)
    {
        var timesheet = await _db.Timesheets.FindAsync([e.TimesheetId], ct);
        if (timesheet != null)
        {
            timesheet.Status = TimesheetStatus.Approved.ToString();
            timesheet.ApprovedAt = e.ApprovedAt;
            timesheet.ApprovedByUserId = e.ApprovedByUserId;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(TimesheetRejectedEvent e, CancellationToken ct)
    {
        var timesheet = await _db.Timesheets.FindAsync([e.TimesheetId], ct);
        if (timesheet != null)
        {
            timesheet.Status = TimesheetStatus.Rejected.ToString();
            timesheet.RejectionReason = e.Reason;
            await _db.SaveChangesAsync(ct);
        }
    }
}

#endregion
