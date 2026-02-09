using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Projects.Domain;

#region Enums

public enum ProjectStatus
{
    Planning = 0,
    InProgress = 1,
    OnHold = 2,
    Completed = 3,
    Cancelled = 4
}

public enum ProjectType
{
    Internal = 0,
    External = 1,
    Research = 2,
    Maintenance = 3
}

public enum ProjectTaskStatus
{
    Open = 0,
    InProgress = 1,
    InReview = 2,
    Completed = 3,
    Cancelled = 4
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

#endregion

#region Value Objects

public record DateRange(DateTime StartDate, DateTime EndDate)
{
    public int DurationDays => (this.EndDate - this.StartDate).Days;
    public bool IsOverdue => DateTime.UtcNow > this.EndDate;
}

public record Budget(decimal PlannedAmount, decimal ActualAmount, string Currency = "CNY")
{
    public decimal Variance => this.PlannedAmount - this.ActualAmount;
    public decimal VariancePercent => this.PlannedAmount > 0 ? (this.Variance / this.PlannedAmount) * 100 : 0;
}

#endregion

#region Domain Events

public record ProjectCreatedEvent(
    Guid ProjectId,
    string ProjectNumber,
    string Name,
    ProjectType Type,
    DateTime StartDate,
    DateTime EndDate,
    decimal Budget,
    string Currency,
    string? CustomerId,
    string ProjectManagerId,
    string? Description
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record ProjectStatusChangedEvent(
    Guid ProjectId,
    ProjectStatus OldStatus,
    ProjectStatus NewStatus
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record TaskAddedEvent(
    Guid ProjectId,
    Guid TaskId,
    string TaskNumber,
    string Title,
    string? Description,
    TaskPriority Priority,
    DateTime? DueDate,
    string? AssigneeId,
    int EstimatedHours
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record TaskStatusChangedEvent(
    Guid ProjectId,
    Guid TaskId,
    ProjectTaskStatus OldStatus,
    ProjectTaskStatus NewStatus,
    int ProgressPercent
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record TaskCompletedEvent(
    Guid ProjectId,
    Guid TaskId,
    DateTime CompletedAt,
    int ActualHours
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record MilestoneAddedEvent(
    Guid ProjectId,
    Guid MilestoneId,
    string Name,
    DateTime DueDate,
    string? Description
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record MilestoneReachedEvent(
    Guid ProjectId,
    Guid MilestoneId,
    DateTime ReachedAt
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record TeamMemberAddedEvent(
    Guid ProjectId,
    string UserId,
    string Role
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record ProjectBudgetUpdatedEvent(
    Guid ProjectId,
    decimal OldBudget,
    decimal NewBudget,
    string? Reason
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

#endregion

#region Entities

public class ProjectTask
{
    public Guid Id { get; private set; }
    public string TaskNumber { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ProjectTaskStatus Status { get; private set; }
    public TaskPriority Priority { get; private set; }
    public DateTime? DueDate { get; private set; }
    public string? AssigneeId { get; private set; }
    public int EstimatedHours { get; private set; }
    public int ActualHours { get; private set; }
    public int ProgressPercent { get; private set; }
    public Guid? ParentTaskId { get; private set; }
    public List<Guid> DependsOnTaskIds { get; private set; } = [];
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public static ProjectTask Create(
        Guid id,
        string taskNumber,
        string title,
        string? description,
        TaskPriority priority,
        DateTime? dueDate,
        string? assigneeId,
        int estimatedHours,
        Guid? parentTaskId = null)
    {
        return new ProjectTask
        {
            Id = id,
            TaskNumber = taskNumber,
            Title = title,
            Description = description,
            Status = ProjectTaskStatus.Open,
            Priority = priority,
            DueDate = dueDate,
            AssigneeId = assigneeId,
            EstimatedHours = estimatedHours,
            ParentTaskId = parentTaskId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProgress(int percent)
    {
        this.ProgressPercent = Math.Clamp(percent, 0, 100);
        switch (this.ProgressPercent)
        {
            case 100 when this.Status != ProjectTaskStatus.Completed:
                this.Status = ProjectTaskStatus.InReview;
                break;
            case > 0 when this.Status == ProjectTaskStatus.Open:
                this.Status = ProjectTaskStatus.InProgress;
                break;
        }
    }

    public void Complete(int actualHours)
    {
        this.Status = ProjectTaskStatus.Completed;
        this.ProgressPercent = 100;
        this.ActualHours = actualHours;
        this.CompletedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(ProjectTaskStatus newStatus)
    {
        this.Status = newStatus;
    }
}

public class Milestone
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime DueDate { get; private set; }
    public bool IsReached { get; private set; }
    public DateTime? ReachedAt { get; private set; }

    public static Milestone Create(Guid id, string name, DateTime dueDate, string? description = null)
    {
        return new Milestone
        {
            Id = id,
            Name = name,
            DueDate = dueDate,
            Description = description,
            IsReached = false
        };
    }

    public void MarkAsReached()
    {
        this.IsReached = true;
        this.ReachedAt = DateTime.UtcNow;
    }
}

public record TeamMember(string UserId, string Role, DateTime JoinedAt);

#endregion

#region Project Aggregate

public class Project : AggregateRoot<Guid>
{
    public string ProjectNumber { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ProjectType Type { get; private set; }
    public ProjectStatus Status { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public decimal PlannedBudget { get; private set; }
    public decimal ActualCost { get; private set; }
    public string Currency { get; private set; } = "CNY";
    public string? CustomerId { get; private set; }
    public string ProjectManagerId { get; private set; } = string.Empty;
    
    public List<ProjectTask> Tasks { get; private set; } = [];
    public List<Milestone> Milestones { get; private set; } = [];
    public List<TeamMember> TeamMembers { get; private set; } = [];

    public int TotalTasks => this.Tasks.Count;
    public int CompletedTasks => this.Tasks.Count(t => t.Status == ProjectTaskStatus.Completed);
    public decimal ProgressPercent => this.TotalTasks > 0 ? (decimal)this.Tasks.Sum(t => t.ProgressPercent) / this.TotalTasks : 0;
    public bool IsOverdue => DateTime.UtcNow > this.EndDate && this.Status != ProjectStatus.Completed;

    public static Project Create(
        Guid id,
        string projectNumber,
        string name,
        ProjectType type,
        DateTime startDate,
        DateTime endDate,
        decimal budget,
        string currency,
        string projectManagerId,
        string? customerId = null,
        string? description = null)
    {
        if (endDate <= startDate)
            throw new ArgumentException("End date must be after start date");

        Project project = new();
        project.ApplyChange(new ProjectCreatedEvent(
            id, projectNumber, name, type, startDate, endDate,
            budget, currency, customerId, projectManagerId, description));
        return project;
    }

    public void ChangeStatus(ProjectStatus newStatus)
    {
        if (this.Status == newStatus) return;
        this.ApplyChange(new ProjectStatusChangedEvent(this.Id, this.Status, newStatus));
    }

    public Guid AddTask(
        string title,
        string? description,
        TaskPriority priority,
        DateTime? dueDate,
        string? assigneeId,
        int estimatedHours,
        Guid? parentTaskId = null)
    {
        Guid taskId = Guid.NewGuid();
        string taskNumber = $"T-{this.Tasks.Count + 1:D3}";
        this.ApplyChange(new TaskAddedEvent(this.Id, taskId, taskNumber, title, description, priority, dueDate, assigneeId, estimatedHours));
        return taskId;
    }

    public void UpdateTaskProgress(Guid taskId, int progressPercent)
    {
        ProjectTask? task = this.Tasks.FirstOrDefault(t => t.Id == taskId);
        if (task == null)
            throw new InvalidOperationException("Task not found");

        ProjectTaskStatus oldStatus = task.Status;
        task.UpdateProgress(progressPercent);
        
        if (oldStatus != task.Status) this.ApplyChange(new TaskStatusChangedEvent(this.Id, taskId, oldStatus, task.Status, progressPercent));
    }

    public void CompleteTask(Guid taskId, int actualHours)
    {
        ProjectTask? task = this.Tasks.FirstOrDefault(t => t.Id == taskId);
        if (task == null)
            throw new InvalidOperationException("Task not found");

        task.Complete(actualHours);
        this.ApplyChange(new TaskCompletedEvent(this.Id, taskId, DateTime.UtcNow, actualHours));
    }

    public Guid AddMilestone(string name, DateTime dueDate, string? description = null)
    {
        Guid milestoneId = Guid.NewGuid();
        this.ApplyChange(new MilestoneAddedEvent(this.Id, milestoneId, name, dueDate, description));
        return milestoneId;
    }

    public void ReachMilestone(Guid milestoneId)
    {
        Milestone? milestone = this.Milestones.FirstOrDefault(m => m.Id == milestoneId);
        if (milestone == null)
            throw new InvalidOperationException("Milestone not found");

        milestone.MarkAsReached();
        this.ApplyChange(new MilestoneReachedEvent(this.Id, milestoneId, DateTime.UtcNow));
    }

    public void AddTeamMember(string userId, string role)
    {
        if (this.TeamMembers.Any(m => m.UserId == userId))
            throw new InvalidOperationException("Team member already exists");

        this.ApplyChange(new TeamMemberAddedEvent(this.Id, userId, role));
    }

    public void UpdateBudget(decimal newBudget, string? reason = null)
    {
        if (newBudget < 0)
            throw new ArgumentException("Budget cannot be negative");

        this.ApplyChange(new ProjectBudgetUpdatedEvent(this.Id, this.PlannedBudget, newBudget, reason));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ProjectCreatedEvent e:
                this.Id = e.ProjectId;
                this.ProjectNumber = e.ProjectNumber;
                this.Name = e.Name;
                this.Type = e.Type;
                this.Status = ProjectStatus.Planning;
                this.StartDate = e.StartDate;
                this.EndDate = e.EndDate;
                this.PlannedBudget = e.Budget;
                this.Currency = e.Currency;
                this.CustomerId = e.CustomerId;
                this.ProjectManagerId = e.ProjectManagerId;
                this.Description = e.Description;
                break;

            case ProjectStatusChangedEvent e:
                this.Status = e.NewStatus;
                break;

            case TaskAddedEvent e:
                ProjectTask task = ProjectTask.Create(
                    e.TaskId, e.TaskNumber, e.Title, e.Description,
                    e.Priority, e.DueDate, e.AssigneeId, e.EstimatedHours);
                this.Tasks.Add(task);
                break;

            case MilestoneAddedEvent e:
                this.Milestones.Add(Milestone.Create(e.MilestoneId, e.Name, e.DueDate, e.Description));
                break;

            case TeamMemberAddedEvent e:
                this.TeamMembers.Add(new TeamMember(e.UserId, e.Role, DateTime.UtcNow));
                break;

            case ProjectBudgetUpdatedEvent e:
                this.PlannedBudget = e.NewBudget;
                break;
        }
    }
}

#endregion
