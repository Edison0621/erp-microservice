using System.Reflection;
using ErpSystem.BuildingBlocks.Domain;
using ErpSystem.Automation.Domain;

namespace ErpSystem.Automation.Application;

/// <summary>
/// Automation Engine - Listens to all domain events and executes matching automation rules
/// </summary>
public class AutomationEngine(
    IEventStore eventStore,
    IAutomationRuleRepository ruleRepository,
    IActionExecutor actionExecutor,
    ILogger<AutomationEngine> logger)
{
    /// <summary>
    /// Process an integration event and execute matching automation rules
    /// </summary>
    public async Task ProcessEvent(IDomainEvent domainEvent)
    {
        string eventType = domainEvent.GetType().Name;

        logger.LogDebug("Processing event {EventType} for automation rules", eventType);

        // Find all active rules that match this event type
        List<AutomationRule> matchingRules = await ruleRepository.GetActiveRulesByEventType(eventType);

        if (!matchingRules.Any())
        {
            logger.LogDebug("No automation rules found for event type {EventType}", eventType);
            return;
        }

        logger.LogInformation(
            "Found {Count} automation rules for event {EventType}",
            matchingRules.Count, eventType);

        foreach (AutomationRule rule in matchingRules)
        {
            try
            {
                await this.ExecuteRule(rule, domainEvent);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to execute automation rule {RuleId} for event {EventType}",
                    rule.Id, eventType);
                
                rule.RecordExecution(success: false, errorMessage: ex.Message);
                await eventStore.SaveAggregateAsync(rule);
            }
        }
    }

    private async Task ExecuteRule(AutomationRule rule, IDomainEvent domainEvent)
    {
        logger.LogInformation("Executing automation rule {RuleName} ({RuleId})", rule.Name, rule.Id);

        // Check condition if specified
        if (rule.Condition != null && !this.EvaluateCondition(rule.Condition, domainEvent))
        {
            logger.LogInformation(
                "Automation rule {RuleName} condition not met, skipping execution",
                rule.Name);
            return;
        }

        // Execute all actions
        bool allSucceeded = true;
        foreach (AutomationAction action in rule.Actions)
        {
            try
            {
                await actionExecutor.ExecuteAction(action, domainEvent);
                logger.LogInformation(
                    "Successfully executed action {ActionType} for rule {RuleName}",
                    action.Type, rule.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to execute action {ActionType} for rule {RuleName}",
                    action.Type, rule.Name);
                allSucceeded = false;
            }
        }

        // Record execution
        rule.RecordExecution(success: allSucceeded);
        await eventStore.SaveAggregateAsync(rule);
    }

    private bool EvaluateCondition(AutomationTriggerCondition condition, IDomainEvent domainEvent)
    {
        // Simple condition evaluation - can be enhanced with expression engine
        try
        {
            Type eventType = domainEvent.GetType();
            PropertyInfo? property = eventType.GetProperty(condition.FieldPath);
            if (property == null)
                return false;

            string? value = property.GetValue(domainEvent)?.ToString();
            
            return condition.Operator.ToLower() switch
            {
                "equals" => value == condition.Value,
                "contains" => value?.Contains(condition.Value) ?? false,
                "greaterthan" => decimal.TryParse(value, out decimal v1) && decimal.TryParse(condition.Value, out decimal v2) && v1 > v2,
                "lessthan" => decimal.TryParse(value, out decimal v3) && decimal.TryParse(condition.Value, out decimal v4) && v3 < v4,
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

public class ActionExecutor(
    IHttpClientFactory httpClientFactory,
    IEmailService emailService,
    INotificationService notificationService,
    ILogger<ActionExecutor> logger)
    : IActionExecutor
{
    public async Task ExecuteAction(AutomationAction action, IDomainEvent triggerEvent)
    {
        logger.LogInformation("Executing action {ActionType}", action.Type);

        switch (action.Type)
        {
            case AutomationActionType.SendEmail:
                await this.ExecuteSendEmail(action);
                break;
            
            case AutomationActionType.SendWebhook:
                await this.ExecuteSendWebhook(action);
                break;
            
            case AutomationActionType.SendNotification:
                await this.ExecuteSendNotification(action);
                break;
            
            case AutomationActionType.CreateRecord:
                await this.ExecuteCreateRecord(action, triggerEvent);
                break;
            
            default:
                logger.LogWarning("Unknown action type: {ActionType}", action.Type);
                break;
        }
    }

    private async Task ExecuteSendEmail(AutomationAction action)
    {
        string to = action.Parameters["to"];
        string subject = action.Parameters["subject"];
        string body = action.Parameters["body"];
        
        await emailService.SendEmailAsync(to, subject, body);
        logger.LogInformation("Sent email to {To}", to);
    }

    private async Task ExecuteSendWebhook(AutomationAction action)
    {
        string url = action.Parameters["url"];
        string payload = action.Parameters["payload"];
        
        HttpClient client = httpClientFactory.CreateClient();
        StringContent content = new(payload, System.Text.Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(url, content);
        
        response.EnsureSuccessStatusCode();
        logger.LogInformation("Sent webhook to {Url}", url);
    }

    private async Task ExecuteSendNotification(AutomationAction action)
    {
        string channel = action.Parameters["channel"];
        string message = action.Parameters["message"];
        
        await notificationService.SendNotificationAsync(channel, message);
        logger.LogInformation("Sent notification to {Channel}", channel);
    }

    private async Task ExecuteCreateRecord(AutomationAction action, IDomainEvent triggerEvent)
    {
        // This would typically dispatch a command to the appropriate service
        logger.LogInformation("Creating record via command dispatch");
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
