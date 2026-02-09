using FluentAssertions;
using ErpSystem.Analytics.Infrastructure;

namespace ErpSystem.IntegrationTests.Analytics;

public class PredictiveAnalyticsTests
{
    private readonly DemandForecastEngine _engine = new();

    [Fact]
    public void DemandForecast_Should_Predict_Trend_Correctly()
    {
        // Setup: Historical data with an upward trend
        List<TimeSeriesData> history = [];
        DateTime startDate = DateTime.UtcNow.AddDays(-30);

        for (int i = 0; i < 30; i++)
        {
            history.Add(new TimeSeriesData 
            { 
                Date = startDate.AddDays(i), 
                Value = 100 + i * 2 // Increasing trend
            });
        }

        // Act: Predict next 7 days
        ForecastResult result = this._engine.PredictDemand(history, 7);

        // Assert
        result.Value.Should().BeGreaterThan(150); // Should follow the upward trend
        result.Confidence.Should().BeInRange(0, 1);
    }

    [Fact]
    public void DemandForecast_Should_Handle_Empty_History()
    {
        // Setup
        List<TimeSeriesData> history = [];

        // Act
        ForecastResult result = this._engine.PredictDemand(history, 7);

        // Assert
        result.Value.Should().Be(0);
    }
}
