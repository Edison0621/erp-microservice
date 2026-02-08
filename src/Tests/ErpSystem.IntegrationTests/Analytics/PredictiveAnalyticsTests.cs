using Xunit;
using FluentAssertions;
using ErpSystem.Analytics.Infrastructure;
using System.Collections.Generic;
using System;

namespace ErpSystem.IntegrationTests.Analytics;

public class PredictiveAnalyticsTests
{
    private readonly DemandForecastEngine _engine;

    public PredictiveAnalyticsTests()
    {
        _engine = new DemandForecastEngine();
    }

    [Fact]
    public void DemandForecast_Should_Predict_Trend_Correctly()
    {
        // Setup: Historical data with an upward trend
        var history = new List<TimeSeriesData>();
        var startDate = DateTime.UtcNow.AddDays(-30);

        for (int i = 0; i < 30; i++)
        {
            history.Add(new TimeSeriesData 
            { 
                Date = startDate.AddDays(i), 
                Value = 100 + i * 2 // Increasing trend
            });
        }

        // Act: Predict next 7 days
        var result = _engine.PredictDemand(history, 7);

        // Assert
        result.Value.Should().BeGreaterThan(150); // Should follow the upward trend
        result.Confidence.Should().BeInRange(0, 1);
    }

    [Fact]
    public void DemandForecast_Should_Handle_Empty_History()
    {
        // Setup
        var history = new List<TimeSeriesData>();

        // Act
        var result = _engine.PredictDemand(history, 7);

        // Assert
        result.Value.Should().Be(0);
    }
}
