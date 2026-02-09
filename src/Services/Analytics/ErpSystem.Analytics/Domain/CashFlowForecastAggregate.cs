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
    public decimal NetPosition => this.PredictedInflow - this.PredictedOutflow;
    public string TenantId { get; private set; } = string.Empty;

    public static CashFlowForecast Create(
        Guid id,
        string tenantId,
        DateTime forecastPeriod,
        decimal inflow,
        decimal outflow)
    {
        CashFlowForecast forecast = new CashFlowForecast();
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
                this.Id = e.AggregateId;
                this.TenantId = e.TenantId;
                this.ForecastPeriod = e.ForecastPeriod;
                this.PredictedInflow = e.PredictedInflow;
                this.PredictedOutflow = e.PredictedOutflow;
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
    public DateTime OccurredOn => this.OccurredAt;
}
