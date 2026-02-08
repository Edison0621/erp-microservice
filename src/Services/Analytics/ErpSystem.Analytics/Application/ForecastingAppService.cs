using ErpSystem.Analytics.Domain;
using ErpSystem.Analytics.Infrastructure;
using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Analytics.Application;

/// <summary>
/// Application service for coordinating forecasting tasks
/// </summary>
public class ForecastingAppService
{
    private readonly DemandForecastEngine _demandEngine;
    private readonly TimescaleDataExtractor _dataExtractor;
    private readonly IEventStore _eventStore;

    public ForecastingAppService(
        DemandForecastEngine demandEngine,
        TimescaleDataExtractor dataExtractor,
        IEventStore eventStore)
    {
        _demandEngine = demandEngine;
        _dataExtractor = dataExtractor;
        _eventStore = eventStore;
    }

    /// <summary>
    /// Executes demand forecast for a material and saves result
    /// </summary>
    public async Task RunMaterialDemandForecast(string tenantId, string materialId, string warehouseId)
    {
        // 1. Extract history (last 90 days)
        var history = await _dataExtractor.GetDailyInventoryMovements(materialId, 90);

        // 2. Predict next 30 days
        var result = _demandEngine.PredictDemand(history, 30);

        // 3. Save forecast aggregate
        var key = $"DF-{materialId}-{warehouseId}-{DateTime.UtcNow:yyyyMMdd}";
        var forecastId = Guid.Parse(string.Format("{0:X32}", key.GetHashCode()));
        
        var forecast = DemandForecast.Create(
            forecastId,
            tenantId,
            materialId,
            warehouseId,
            result.Value,
            DateTime.UtcNow.AddDays(30),
            result.Confidence);

        await _eventStore.SaveAggregateAsync(forecast);
    }
}
