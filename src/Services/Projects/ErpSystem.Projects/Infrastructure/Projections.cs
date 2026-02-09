using MediatR;
using System.Text.Json;
using ErpSystem.Projects.Domain;

namespace ErpSystem.Projects.Infrastructure;

#region Project Projections

public class ProjectProjectionHandler(ProjectsReadDbContext db) :
    INotificationHandler<ProjectCreatedEvent>,
    INotificationHandler<ProjectStatusChangedEvent>,
    INotificationHandler<TaskAddedEvent>,
    INotificationHandler<TaskCompletedEvent>,
    INotificationHandler<MilestoneAddedEvent>,
    INotificationHandler<MilestoneReachedEvent>,
    INotificationHandler<TeamMemberAddedEvent>,
    INotificationHandler<ProjectBudgetUpdatedEvent>
{
    public async Task Handle(ProjectCreatedEvent e, CancellationToken ct)
    {
        ProjectReadModel project = new()
        {
            Id = e.ProjectId,
            ProjectNumber = e.ProjectNumber,
            Name = e.Name,
            Description = e.Description,
            Type = e.Type.ToString(),
            Status = nameof(ProjectStatus.Planning),
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
        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);
    }

    public async Task Handle(ProjectStatusChangedEvent e, CancellationToken ct)
    {
        ProjectReadModel? project = await db.Projects.FindAsync([e.ProjectId], ct);
        if (project != null)
        {
            project.Status = e.NewStatus.ToString();
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(TaskAddedEvent e, CancellationToken ct)
    {
        TaskReadModel task = new()
        {
            Id = e.TaskId,
            ProjectId = e.ProjectId,
            TaskNumber = e.TaskNumber,
            Title = e.Title,
            Description = e.Description,
            Status = nameof(ProjectTaskStatus.Open),
            Priority = e.Priority.ToString(),
            DueDate = e.DueDate,
            AssigneeId = e.AssigneeId,
            EstimatedHours = e.EstimatedHours,
            CreatedAt = DateTime.UtcNow
        };
        db.Tasks.Add(task);

        ProjectReadModel? project = await db.Projects.FindAsync([e.ProjectId], ct);
        if (project != null)
        {
            project.TotalTasks++;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task Handle(TaskCompletedEvent e, CancellationToken ct)
    {
        TaskReadModel? task = await db.Tasks.FindAsync([e.TaskId], ct);
        if (task != null)
        {
            task.Status = nameof(ProjectTaskStatus.Completed);
            task.ProgressPercent = 100;
            task.ActualHours = e.ActualHours;
            task.CompletedAt = e.CompletedAt;
        }

        ProjectReadModel? project = await db.Projects.FindAsync([e.ProjectId], ct);
        if (project != null)
        {
            project.CompletedTasks++;
            project.ProgressPercent = project.TotalTasks > 0 
                ? (decimal)project.CompletedTasks / project.TotalTasks * 100 : 0;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task Handle(MilestoneAddedEvent e, CancellationToken ct)
    {
        ProjectReadModel? project = await db.Projects.FindAsync([e.ProjectId], ct);
        if (project != null)
        {
            List<object> milestones = JsonSerializer.Deserialize<List<object>>(project.Milestones) ?? [];
            milestones.Add(new { e.MilestoneId, e.Name, e.DueDate, e.Description, IsReached = false });
            project.Milestones = JsonSerializer.Serialize(milestones);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(MilestoneReachedEvent e, CancellationToken ct)
    {
        // Update milestone in JSON - simplified for now
        await Task.CompletedTask;
    }

    public async Task Handle(TeamMemberAddedEvent e, CancellationToken ct)
    {
        ProjectReadModel? project = await db.Projects.FindAsync([e.ProjectId], ct);
        if (project != null)
        {
            List<TeamMember> members = JsonSerializer.Deserialize<List<TeamMember>>(project.TeamMembers) ?? [];
            members.Add(new TeamMember(e.UserId, e.Role, DateTime.UtcNow));
            project.TeamMembers = JsonSerializer.Serialize(members);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(ProjectBudgetUpdatedEvent e, CancellationToken ct)
    {
        ProjectReadModel? project = await db.Projects.FindAsync([e.ProjectId], ct);
        if (project != null)
        {
            project.PlannedBudget = e.NewBudget;
            await db.SaveChangesAsync(ct);
        }
    }
}

#endregion

#region Timesheet Projections

public class TimesheetProjectionHandler(ProjectsReadDbContext db) :
    INotificationHandler<TimesheetCreatedEvent>,
    INotificationHandler<TimesheetEntryAddedEvent>,
    INotificationHandler<TimesheetSubmittedEvent>,
    INotificationHandler<TimesheetApprovedEvent>,
    INotificationHandler<TimesheetRejectedEvent>
{
    public async Task Handle(TimesheetCreatedEvent e, CancellationToken ct)
    {
        TimesheetReadModel timesheet = new()
        {
            Id = e.TimesheetId,
            TimesheetNumber = e.TimesheetNumber,
            ProjectId = e.ProjectId,
            UserId = e.UserId,
            WeekStartDate = e.WeekStartDate,
            WeekEndDate = e.WeekEndDate,
            Status = nameof(TimesheetStatus.Draft),
            Entries = "[]"
        };
        db.Timesheets.Add(timesheet);
        await db.SaveChangesAsync(ct);
    }

    public async Task Handle(TimesheetEntryAddedEvent e, CancellationToken ct)
    {
        TimesheetReadModel? timesheet = await db.Timesheets.FindAsync([e.TimesheetId], ct);
        if (timesheet != null)
        {
            List<object> entries = JsonSerializer.Deserialize<List<object>>(timesheet.Entries) ?? [];
            entries.Add(new { e.EntryId, e.TaskId, e.WorkDate, e.Hours, e.Description });
            timesheet.Entries = JsonSerializer.Serialize(entries);
            timesheet.TotalHours += e.Hours;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(TimesheetSubmittedEvent e, CancellationToken ct)
    {
        TimesheetReadModel? timesheet = await db.Timesheets.FindAsync([e.TimesheetId], ct);
        if (timesheet != null)
        {
            timesheet.Status = nameof(TimesheetStatus.Submitted);
            timesheet.SubmittedAt = e.SubmittedAt;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(TimesheetApprovedEvent e, CancellationToken ct)
    {
        TimesheetReadModel? timesheet = await db.Timesheets.FindAsync([e.TimesheetId], ct);
        if (timesheet != null)
        {
            timesheet.Status = nameof(TimesheetStatus.Approved);
            timesheet.ApprovedAt = e.ApprovedAt;
            timesheet.ApprovedByUserId = e.ApprovedByUserId;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(TimesheetRejectedEvent e, CancellationToken ct)
    {
        TimesheetReadModel? timesheet = await db.Timesheets.FindAsync([e.TimesheetId], ct);
        if (timesheet != null)
        {
            timesheet.Status = nameof(TimesheetStatus.Rejected);
            timesheet.RejectionReason = e.Reason;
            await db.SaveChangesAsync(ct);
        }
    }
}

#endregion
