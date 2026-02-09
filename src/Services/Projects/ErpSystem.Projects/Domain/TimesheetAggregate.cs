using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Projects.Domain;

#region Domain Events

public record TimesheetCreatedEvent(
    Guid TimesheetId,
    string TimesheetNumber,
    Guid ProjectId,
    string UserId,
    DateTime WeekStartDate,
    DateTime WeekEndDate
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record TimesheetEntryAddedEvent(
    Guid TimesheetId,
    Guid EntryId,
    Guid TaskId,
    DateTime WorkDate,
    decimal Hours,
    string? Description
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record TimesheetSubmittedEvent(
    Guid TimesheetId,
    decimal TotalHours,
    DateTime SubmittedAt
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record TimesheetApprovedEvent(
    Guid TimesheetId,
    string ApprovedByUserId,
    DateTime ApprovedAt
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record TimesheetRejectedEvent(
    Guid TimesheetId,
    string RejectedByUserId,
    string Reason
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

#endregion

#region Enums

public enum TimesheetStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3
}

#endregion

#region Entities

public class TimesheetEntry
{
    public Guid Id { get; private set; }
    public Guid TaskId { get; private set; }
    public DateTime WorkDate { get; private set; }
    public decimal Hours { get; private set; }
    public string? Description { get; private set; }

    public static TimesheetEntry Create(Guid id, Guid taskId, DateTime workDate, decimal hours, string? description)
    {
        if (hours <= 0 || hours > 24)
            throw new ArgumentException("Hours must be between 0 and 24");

        return new TimesheetEntry
        {
            Id = id,
            TaskId = taskId,
            WorkDate = workDate,
            Hours = hours,
            Description = description
        };
    }
}

#endregion

#region Timesheet Aggregate

public class Timesheet : AggregateRoot<Guid>
{
    public string TimesheetNumber { get; private set; } = string.Empty;
    public Guid ProjectId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public DateTime WeekStartDate { get; private set; }
    public DateTime WeekEndDate { get; private set; }
    public TimesheetStatus Status { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? ApprovedByUserId { get; private set; }
    public string? RejectionReason { get; private set; }
    
    public List<TimesheetEntry> Entries { get; private set; } = [];
    
    public decimal TotalHours => this.Entries.Sum(e => e.Hours);

    public static Timesheet Create(
        Guid id,
        string timesheetNumber,
        Guid projectId,
        string userId,
        DateTime weekStartDate)
    {
        DateTime weekEndDate = weekStartDate.AddDays(6);
        Timesheet timesheet = new();
        timesheet.ApplyChange(new TimesheetCreatedEvent(
            id, timesheetNumber, projectId, userId, weekStartDate, weekEndDate));
        return timesheet;
    }

    public void AddEntry(Guid taskId, DateTime workDate, decimal hours, string? description = null)
    {
        if (this.Status != TimesheetStatus.Draft && this.Status != TimesheetStatus.Rejected)
            throw new InvalidOperationException("Cannot add entries to submitted or approved timesheet");

        if (workDate < this.WeekStartDate || workDate > this.WeekEndDate)
            throw new ArgumentException("Work date must be within the timesheet week");

        Guid entryId = Guid.NewGuid();
        this.ApplyChange(new TimesheetEntryAddedEvent(this.Id, entryId, taskId, workDate, hours, description));
    }

    public void Submit()
    {
        if (this.Status != TimesheetStatus.Draft && this.Status != TimesheetStatus.Rejected)
            throw new InvalidOperationException("Only draft or rejected timesheets can be submitted");

        if (!this.Entries.Any())
            throw new InvalidOperationException("Cannot submit empty timesheet");

        this.ApplyChange(new TimesheetSubmittedEvent(this.Id, this.TotalHours, DateTime.UtcNow));
    }

    public void Approve(string approvedByUserId)
    {
        if (this.Status != TimesheetStatus.Submitted)
            throw new InvalidOperationException("Only submitted timesheets can be approved");

        this.ApplyChange(new TimesheetApprovedEvent(this.Id, approvedByUserId, DateTime.UtcNow));
    }

    public void Reject(string rejectedByUserId, string reason)
    {
        if (this.Status != TimesheetStatus.Submitted)
            throw new InvalidOperationException("Only submitted timesheets can be rejected");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required");

        this.ApplyChange(new TimesheetRejectedEvent(this.Id, rejectedByUserId, reason));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case TimesheetCreatedEvent e:
                this.Id = e.TimesheetId;
                this.TimesheetNumber = e.TimesheetNumber;
                this.ProjectId = e.ProjectId;
                this.UserId = e.UserId;
                this.WeekStartDate = e.WeekStartDate;
                this.WeekEndDate = e.WeekEndDate;
                this.Status = TimesheetStatus.Draft;
                break;

            case TimesheetEntryAddedEvent e:
                this.Entries.Add(TimesheetEntry.Create(e.EntryId, e.TaskId, e.WorkDate, e.Hours, e.Description));
                break;

            case TimesheetSubmittedEvent e:
                this.Status = TimesheetStatus.Submitted;
                this.SubmittedAt = e.SubmittedAt;
                break;

            case TimesheetApprovedEvent e:
                this.Status = TimesheetStatus.Approved;
                this.ApprovedAt = e.ApprovedAt;
                this.ApprovedByUserId = e.ApprovedByUserId;
                break;

            case TimesheetRejectedEvent e:
                this.Status = TimesheetStatus.Rejected;
                this.RejectionReason = e.Reason;
                break;
        }
    }
}

#endregion
