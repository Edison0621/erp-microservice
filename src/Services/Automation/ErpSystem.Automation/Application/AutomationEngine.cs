using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Automation.Domain;
using Microsoft.Extensions.Logging;

namespace ErpSystem.Automation.Application;

/// <summary>
/// Automation Engine - Listens to all domain events and executes matching automation rules
/// </summary>
public class AutomationEngine
{
    private readonly IEventStore _eventStore;
    private readonly IAutomationRuleRepository _ruleRepository;
    private readonly IActionExecutor _actionExecutor;
    private readonly ILogger<AutomationEngine> _logger;

    public AutomationEngine(
        IEventStore eventStore,
        IAutomationRuleRepository ruleRepository,
        IActionExecutor actionExecutor,
        ILogger<AutomationEngine> logger)
    {
        _eventStore = eventStore;
        _ruleRepository = ruleRepository;
        _actionExecutor = actionExecutor;
        _logger = logger;
    }

    /// <summary>
    /// Process an integration event and execute matching automation rules
    /// </summary>
    public async Task ProcessEvent(IDomainEvent domainEvent)
    {
        var eventType = domainEvent.GetType().Name;
        
        _logger.LogDebug("Processing event {EventType} for automation rules", eventType);

        // Find all active rules that match this event type
        var matchingRules = await _ruleRepository.GetActiveRulesByEventType(eventType);

        if (!matchingRules.Any())
        {
            _logger.LogDebug("No automation rules found for event type {EventType}", eventType);
            return;
        }

        _logger.LogInformation(
            "Found {Count} automation rules for event {EventType}",
            matchingRules.Count, eventType);

        foreach (var rule in matchingRules)
        {
            try
            {
                await ExecuteRule(rule, domainEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to execute automation rule {RuleId} for event {EventType}",
                    rule.Id, eventType);
                
                rule.RecordExecution(success: false, errorMessage: ex.Message);
                await _eventStore.SaveAggregateAsync(rule);
            }
        }
    }

    private async Task ExecuteRule(AutomationRule rule, IDomainEvent domainEvent)
    {
        _logger.LogInformation("Executing automation rule {RuleName} ({RuleId})", rule.Name, rule.Id);

        // Check condition if specified
        if (rule.Condition != null && !EvaluateCondition(rule.Condition, domainEvent))
        {
            _logger.LogInformation(
                "Automation rule {RuleName} condition not met, skipping execution",
                rule.Name);
            return;
        }

        // Execute all actions
        var allSucceeded = true;
        foreach (var action in rule.Actions)
        {
            try
            {
                await _actionExecutor.ExecuteAction(action, domainEvent);
                _logger.LogInformation(
                    "Successfully executed action {ActionType} for rule {RuleName}",
                    action.Type, rule.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to execute action {ActionType} for rule {RuleName}",
                    action.Type, rule.Name);
                allSucceeded = false;
            }
        }

        // Record execution
        rule.RecordExecution(success: allSucceeded);
        await _eventStore.SaveAggregateAsync(rule);
    }

    private bool EvaluateCondition(AutomationTriggerCondition condition, IDomainEvent domainEvent)
    {
        // Simple condition evaluation - can be enhanced with expression engine
        try
        {
            var eventType = domainEvent.GetType();
            var property = eventType.GetProperty(condition.FieldPath);
            if (property == null)
                return false;

            var value = property.GetValue(domainEvent)?.ToString();
            
            return condition.Operator.ToLower() switch
            {
                "equals" => value == condition.Value,
                "contains" => value?.Contains(condition.Value) ?? false,
                "greaterthan" => decimal.TryParse(value, out var v1) && decimal.TryParse(condition.Value, out var v2) && v1 > v2,
                "lessthan" => decimal.TryParse(value, out var v3) && decimal.TryParse(condition.Value, out var v4) && v3 < v4,
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Action Executor - Executes individual automation actions
/// </summary>
public interface IActionExecutor
{
    Task ExecuteAction(AutomationAction action, IDomainEvent triggerEvent);
}

public class ActionExecutor : IActionExecutor
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ActionExecutor> _logger;

    public ActionExecutor(
        IHttpClientFactory httpClientFactory,
        IEmailService emailService,
        INotificationService notificationService,
        ILogger<ActionExecutor> logger)
    {
        _httpClientFactory = httpClientFactory;
        _emailService = emailService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task ExecuteAction(AutomationAction action, IDomainEvent triggerEvent)
    {
        _logger.LogInformation("Executing action {ActionType}", action.Type);

        switch (action.Type)
        {
            case AutomationActionType.SendEmail:
                await ExecuteSendEmail(action);
                break;
            
            case AutomationActionType.SendWebhook:
                await ExecuteSendWebhook(action);
                break;
            
            case AutomationActionType.SendNotification:
                await ExecuteSendNotification(action);
                break;
            
            case AutomationActionType.CreateRecord:
                await ExecuteCreateRecord(action, triggerEvent);
                break;
            
            default:
                _logger.LogWarning("Unknown action type: {ActionType}", action.Type);
                break;
        }
    }

    private async Task ExecuteSendEmail(AutomationAction action)
    {
        var to = action.Parameters["to"];
        var subject = action.Parameters["subject"];
        var body = action.Parameters["body"];
        
        await _emailService.SendEmailAsync(to, subject, body);
        _logger.LogInformation("Sent email to {To}", to);
    }

    private async Task ExecuteSendWebhook(AutomationAction action)
    {
        var url = action.Parameters["url"];
        var payload = action.Parameters["payload"];
        
        var client = _httpClientFactory.CreateClient();
        var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        
        response.EnsureSuccessStatusCode();
        _logger.LogInformation("Sent webhook to {Url}", url);
    }

    private async Task ExecuteSendNotification(AutomationAction action)
    {
        var channel = action.Parameters["channel"];
        var message = action.Parameters["message"];
        
        await _notificationService.SendNotificationAsync(channel, message);
        _logger.LogInformation("Sent notification to {Channel}", channel);
    }

    private async Task ExecuteCreateRecord(AutomationAction action, IDomainEvent triggerEvent)
    {
        // This would typically dispatch a command to the appropriate service
        _logger.LogInformation("Creating record via command dispatch");
        await Task.CompletedTask;
    }
}

// Service Interfaces
public interface IAutomationRuleRepository
{
    Task<List<AutomationRule>> GetActiveRulesByEventType(string eventType);
}

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}

public interface INotificationService
{
    Task SendNotificationAsync(string channel, string message);
}
