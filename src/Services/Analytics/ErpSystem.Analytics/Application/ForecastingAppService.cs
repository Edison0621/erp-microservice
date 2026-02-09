using ErpSystem.Analytics.Domain;
using ErpSystem.Analytics.Infrastructure;
using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Analytics.Application;

/// <summary>
/// Application service for coordinating forecasting tasks
/// </summary>
public class ForecastingAppService(
    DemandForecastEngine demandEngine,
    TimescaleDataExtractor dataExtractor,
    IEventStore eventStore)
{
    /// <summary>
    /// Executes demand forecast for a material and saves result
    /// </summary>
    public async Task RunMaterialDemandForecast(string tenantId, string materialId, string warehouseId)
    {
        // 1. Extract history (last 90 days)
        List<TimeSeriesData> history = await dataExtractor.GetDailyInventoryMovements(materialId, 90);

        // 2. Predict next 30 days
        ForecastResult result = demandEngine.PredictDemand(history, 30);

        // 3. Save forecast aggregate
        string key = $"DF-{materialId}-{warehouseId}-{DateTime.UtcNow:yyyyMMdd}";
        Guid forecastId = Guid.Parse($"{key.GetHashCode():X32}");
        
        DemandForecast forecast = DemandForecast.Create(
            forecastId,
            tenantId,
            materialId,
            warehouseId,
            result.Value,
            DateTime.UtcNow.AddDays(30),
            result.Confidence);

        await eventStore.SaveAggregateAsync(forecast);
    }
}
