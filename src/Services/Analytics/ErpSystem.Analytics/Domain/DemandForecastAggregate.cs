using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Analytics.Domain;

/// <summary>
/// Demand Forecast Aggregate - Manages predicted demand for a specific material and warehouse
/// </summary>
public class DemandForecast : AggregateRoot<Guid>
{
    public string MaterialId { get; private set; } = string.Empty;
    public string WarehouseId { get; private set; } = string.Empty;
    public decimal PredictedQuantity { get; private set; }
    public DateTime ForecastDate { get; private set; }
    public double ConfidenceScore { get; private set; }
    public string TenantId { get; private set; } = string.Empty;

    public static DemandForecast Create(
        Guid id,
        string tenantId,
        string materialId,
        string warehouseId,
        decimal predictedQuantity,
        DateTime forecastDate,
        double confidenceScore)
    {
        DemandForecast forecast = new();
        forecast.ApplyChange(new DemandForecastCreatedEvent(
            id,
            tenantId,
            materialId,
            warehouseId,
            predictedQuantity,
            forecastDate,
            confidenceScore,
            DateTime.UtcNow));
        return forecast;
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case DemandForecastCreatedEvent e:
                this.Id = e.AggregateId;
                this.TenantId = e.TenantId;
                this.MaterialId = e.MaterialId;
                this.WarehouseId = e.WarehouseId;
                this.PredictedQuantity = e.PredictedQuantity;
                this.ForecastDate = e.ForecastDate;
                this.ConfidenceScore = e.ConfidenceScore;
                break;
        }
    }
}

public record DemandForecastCreatedEvent(
    Guid AggregateId,
    string TenantId,
    string MaterialId,
    string WarehouseId,
    decimal PredictedQuantity,
    DateTime ForecastDate,
    double ConfidenceScore,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn => this.OccurredAt;
}
