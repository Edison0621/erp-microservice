using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Analytics.Domain;

/// <summary>
/// Cash Flow Forecast Aggregate - Manages predicted financial liquidity
/// </summary>
public class CashFlowForecast : AggregateRoot<Guid>
{
    public DateTime ForecastPeriod { get; private set; }
    public decimal PredictedInflow { get; private set; }
    public decimal PredictedOutflow { get; private set; }
    public decimal NetPosition => PredictedInflow - PredictedOutflow;
    public string TenantId { get; private set; } = string.Empty;

    public static CashFlowForecast Create(
        Guid id,
        string tenantId,
        DateTime forecastPeriod,
        decimal inflow,
        decimal outflow)
    {
        var forecast = new CashFlowForecast();
        forecast.ApplyChange(new CashFlowForecastCreatedEvent(
            id,
            tenantId,
            forecastPeriod,
            inflow,
            outflow,
            DateTime.UtcNow));
        return forecast;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case CashFlowForecastCreatedEvent e:
                Id = e.AggregateId;
                TenantId = e.TenantId;
                ForecastPeriod = e.ForecastPeriod;
                PredictedInflow = e.PredictedInflow;
                PredictedOutflow = e.PredictedOutflow;
                break;
        }
    }
}

public record CashFlowForecastCreatedEvent(
    Guid AggregateId,
    string TenantId,
    DateTime ForecastPeriod,
    decimal PredictedInflow,
    decimal PredictedOutflow,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => OccurredAt;
}
