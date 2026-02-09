using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Automation.Domain;

/// <summary>
/// Automation Rule Aggregate - Defines event-driven automation workflows
/// </summary>
public class AutomationRule : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string TriggerEventType { get; private set; } = string.Empty;
    public List<AutomationAction> Actions { get; private set; } = [];
    public bool IsActive { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public AutomationTriggerCondition? Condition { get; private set; }
    public int ExecutionCount { get; private set; }
    public DateTime? LastExecutedAt { get; private set; }

    public static AutomationRule Create(
        Guid id,
        string tenantId,
        string name,
        string description,
        string triggerEventType,
        AutomationTriggerCondition? condition = null)
    {
        AutomationRule rule = new();
        rule.ApplyChange(new AutomationRuleCreatedEvent(
            id,
            tenantId,
            name,
            description,
            triggerEventType,
            condition,
            DateTime.UtcNow));
        return rule;
    }

    public void AddAction(AutomationAction action)
    {
        if (this.Actions.Any(a => a.Id == action.Id))
            throw new InvalidOperationException($"Action {action.Id} already exists");

        this.ApplyChange(new AutomationActionAddedEvent(this.Id, action, DateTime.UtcNow));
    }

    public void RemoveAction(string actionId)
    {
        if (this.Actions.All(a => a.Id != actionId))
            throw new InvalidOperationException($"Action {actionId} not found");

        this.ApplyChange(new AutomationActionRemovedEvent(this.Id, actionId, DateTime.UtcNow));
    }

    public void Activate()
    {
        if (this.IsActive)
            return;

        this.ApplyChange(new AutomationRuleActivatedEvent(this.Id, DateTime.UtcNow));
    }

    public void Deactivate()
    {
        if (!this.IsActive)
            return;

        this.ApplyChange(new AutomationRuleDeactivatedEvent(this.Id, DateTime.UtcNow));
    }

    public void RecordExecution(bool success, string? errorMessage = null)
    {
        this.ApplyChange(new AutomationRuleExecutedEvent(this.Id,
            success,
            errorMessage,
            DateTime.UtcNow));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case AutomationRuleCreatedEvent e:
                this.Id = e.AggregateId;
                this.TenantId = e.TenantId;
                this.Name = e.Name;
                this.Description = e.Description;
                this.TriggerEventType = e.TriggerEventType;
                this.Condition = e.Condition;
                this.IsActive = true;
                break;
            case AutomationActionAddedEvent e:
                this.Actions.Add(e.Action);
                break;
            case AutomationActionRemovedEvent e:
                this.Actions.RemoveAll(a => a.Id == e.ActionId);
                break;
            case AutomationRuleActivatedEvent:
                this.IsActive = true;
                break;
            case AutomationRuleDeactivatedEvent:
                this.IsActive = false;
                break;
            case AutomationRuleExecutedEvent e:
                this.ExecutionCount++;
                this.LastExecutedAt = e.OccurredAt;
                break;
        }
    }
}

/// <summary>
/// Trigger condition for automation rule
/// </summary>
public record AutomationTriggerCondition(
    string FieldPath,
    string Operator,
    string Value);

/// <summary>
/// Action to be executed when rule is triggered
/// </summary>
public record AutomationAction(
    string Id,
    AutomationActionType Type,
    Dictionary<string, string> Parameters);

public enum AutomationActionType
{
    SendEmail = 1,
    SendWebhook = 2,
    CreateRecord = 3,
    UpdateRecord = 4,
    SendNotification = 5,
    ExecuteCommand = 6
}

// Domain Events
public record AutomationRuleCreatedEvent(
    Guid AggregateId,
    string TenantId,
    string Name,
    string Description,
    string TriggerEventType,
    AutomationTriggerCondition? Condition,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => this.OccurredAt;
}

public record AutomationActionAddedEvent(
    Guid AggregateId,
    AutomationAction Action,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => this.OccurredAt;
}

public record AutomationActionRemovedEvent(
    Guid AggregateId,
    string ActionId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => this.OccurredAt;
}

public record AutomationRuleActivatedEvent(
    Guid AggregateId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => this.OccurredAt;
}

public record AutomationRuleDeactivatedEvent(
    Guid AggregateId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => this.OccurredAt;
}

public record AutomationRuleExecutedEvent(
    Guid AggregateId,
    bool Success,
    string? ErrorMessage,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => this.OccurredAt;
}
