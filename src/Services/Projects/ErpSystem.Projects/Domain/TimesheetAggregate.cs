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
    
    public List<TimesheetEntry> Entries { get; private set; } = new();
    
    public decimal TotalHours => Entries.Sum(e => e.Hours);

    public static Timesheet Create(
        Guid id,
        string timesheetNumber,
        Guid projectId,
        string userId,
        DateTime weekStartDate)
    {
        var weekEndDate = weekStartDate.AddDays(6);
        var timesheet = new Timesheet();
        timesheet.ApplyChange(new TimesheetCreatedEvent(
            id, timesheetNumber, projectId, userId, weekStartDate, weekEndDate));
        return timesheet;
    }

    public void AddEntry(Guid taskId, DateTime workDate, decimal hours, string? description = null)
    {
        if (Status != TimesheetStatus.Draft && Status != TimesheetStatus.Rejected)
            throw new InvalidOperationException("Cannot add entries to submitted or approved timesheet");

        if (workDate < WeekStartDate || workDate > WeekEndDate)
            throw new ArgumentException("Work date must be within the timesheet week");

        var entryId = Guid.NewGuid();
        ApplyChange(new TimesheetEntryAddedEvent(Id, entryId, taskId, workDate, hours, description));
    }

    public void Submit()
    {
        if (Status != TimesheetStatus.Draft && Status != TimesheetStatus.Rejected)
            throw new InvalidOperationException("Only draft or rejected timesheets can be submitted");

        if (!Entries.Any())
            throw new InvalidOperationException("Cannot submit empty timesheet");

        ApplyChange(new TimesheetSubmittedEvent(Id, TotalHours, DateTime.UtcNow));
    }

    public void Approve(string approvedByUserId)
    {
        if (Status != TimesheetStatus.Submitted)
            throw new InvalidOperationException("Only submitted timesheets can be approved");

        ApplyChange(new TimesheetApprovedEvent(Id, approvedByUserId, DateTime.UtcNow));
    }

    public void Reject(string rejectedByUserId, string reason)
    {
        if (Status != TimesheetStatus.Submitted)
            throw new InvalidOperationException("Only submitted timesheets can be rejected");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required");

        ApplyChange(new TimesheetRejectedEvent(Id, rejectedByUserId, reason));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case TimesheetCreatedEvent e:
                Id = e.TimesheetId;
                TimesheetNumber = e.TimesheetNumber;
                ProjectId = e.ProjectId;
                UserId = e.UserId;
                WeekStartDate = e.WeekStartDate;
                WeekEndDate = e.WeekEndDate;
                Status = TimesheetStatus.Draft;
                break;

            case TimesheetEntryAddedEvent e:
                Entries.Add(TimesheetEntry.Create(e.EntryId, e.TaskId, e.WorkDate, e.Hours, e.Description));
                break;

            case TimesheetSubmittedEvent e:
                Status = TimesheetStatus.Submitted;
                SubmittedAt = e.SubmittedAt;
                break;

            case TimesheetApprovedEvent e:
                Status = TimesheetStatus.Approved;
                ApprovedAt = e.ApprovedAt;
                ApprovedByUserId = e.ApprovedByUserId;
                break;

            case TimesheetRejectedEvent e:
                Status = TimesheetStatus.Rejected;
                RejectionReason = e.Reason;
                break;
        }
    }
}

#endregion
