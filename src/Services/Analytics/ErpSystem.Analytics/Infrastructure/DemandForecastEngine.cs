using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;

namespace ErpSystem.Analytics.Infrastructure;

/// <summary>
/// Forecasting Engine using ML.NET for time-series demand prediction
/// </summary>
public class DemandForecastEngine
{
    private readonly MLContext _mlContext = new();

    public ForecastResult PredictDemand(IEnumerable<TimeSeriesData> history, int horizon)
    {
        if (!history.Any()) return new ForecastResult(0, 0);

        // Load data into ML.NET
        IDataView dataView = this._mlContext.Data.LoadFromEnumerable(history);

        // Setup forecasting pipeline (Singular Spectrum Analysis)
        SsaForecastingEstimator? forecastingPipeline = this._mlContext.Forecasting.ForecastBySsa(
            outputColumnName: "Forecast",
            inputColumnName: nameof(TimeSeriesData.Value),
            windowSize: Math.Min(history.Count() / 2, 7), // Weekly seasonality check
            seriesLength: history.Count(),
            trainSize: history.Count(),
            horizon: horizon,
            confidenceLevel: 0.95f,
            confidenceLowerBoundColumn: "LowerBound",
            confidenceUpperBoundColumn: "UpperBound");

        // Train model
        SsaForecastingTransformer? trainedModel = forecastingPipeline.Fit(dataView);

        // Create prediction engine
        TimeSeriesPredictionEngine<TimeSeriesData, TimeSeriesForecast>? forecastingEngine = trainedModel.CreateTimeSeriesEngine<TimeSeriesData, TimeSeriesForecast>(this._mlContext);

        // Predict
        TimeSeriesForecast? forecast = forecastingEngine.Predict();

        return new ForecastResult(
            (decimal)forecast.Forecast[0],
            0.95 // Default confidence for this model
        );
    }
}

public class TimeSeriesData
{
    public DateTime Date { get; set; }
    public float Value { get; set; }
}

public class TimeSeriesForecast
{
    public float[] Forecast { get; set; }
    public float[] LowerBound { get; set; }
    public float[] UpperBound { get; set; }
}

public record ForecastResult(decimal Value, double Confidence);
