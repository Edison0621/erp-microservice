using Npgsql;

namespace ErpSystem.Analytics.Infrastructure;

/// <summary>
/// Service to extract historical data from TimescaleDB for AI features
/// </summary>
public class TimescaleDataExtractor(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("AnalyticsConnection") ?? throw new ArgumentNullException("AnalyticsConnection");

    /// <summary>
    /// Extracts aggregated daily inventory movements for a specific material
    /// </summary>
    public async Task<List<TimeSeriesData>> GetDailyInventoryMovements(string materialId, int pastDays)
    {
        List<TimeSeriesData> result = [];

        using NpgsqlConnection conn = new NpgsqlConnection(this._connectionString);
        await conn.OpenAsync();

        // Query the inventory_transactions_ts hypertable
        using NpgsqlCommand cmd = new NpgsqlCommand(
            @"SELECT 
                time_bucket('1 day', time) AS bucket,
                ABS(SUM(quantity_change)) AS total_usage
              FROM inventory_transactions_ts
              WHERE material_id = @materialId 
                AND time > NOW() - @pastDays * INTERVAL '1 day'
                AND quantity_change < 0
              GROUP BY bucket
              ORDER BY bucket ASC;", conn);

        cmd.Parameters.AddWithValue("materialId", materialId);
        cmd.Parameters.AddWithValue("pastDays", pastDays);

        using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new TimeSeriesData
            {
                Date = reader.GetDateTime(0),
                Value = (float)reader.GetDecimal(1)
            });
        }

        return result;
    }

    /// <summary>
    /// Extracts historical cash flow data
    /// </summary>
    public async Task<List<TimeSeriesData>> GetDailyCashFlow(int pastDays)
    {
        List<TimeSeriesData> result = [];

        using NpgsqlConnection conn = new NpgsqlConnection(this._connectionString);
        await conn.OpenAsync();

        // Query cost movements or a dedicated financial transaction hypertable
        using NpgsqlCommand cmd = new NpgsqlCommand(
            @"SELECT 
                time_bucket('1 day', time) AS bucket,
                SUM(total_value) AS daily_value
              FROM cost_movements_ts
              WHERE time > NOW() - @pastDays * INTERVAL '1 day'
              GROUP BY bucket
              ORDER BY bucket ASC;", conn);

        cmd.Parameters.AddWithValue("pastDays", pastDays);

        using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new TimeSeriesData
            {
                Date = reader.GetDateTime(0),
                Value = (float)reader.GetDecimal(1)
            });
        }

        return result;
    }

    /// <summary>
    /// Extracts real-time advanced statistics from TimescaleDB aggregates
    /// </summary>
    public async Task<List<MaterialStatsDto>> GetRealTimeStats()
    {
        List<MaterialStatsDto> result = [];

        using NpgsqlConnection conn = new NpgsqlConnection(this._connectionString);
        await conn.OpenAsync();

        try
        {
             // Use TimescaleDB Toolkit functions to access the aggregates
            using NpgsqlCommand cmd = new NpgsqlCommand(
                @"SELECT 
                    hour,
                    material_id,
                    approx_percentile(quantity_distribution, 0.5) as median_change,
                    average(rolling_stats) as avg_change,
                    stddev(rolling_stats) as stddev_change
                  FROM inventory_advanced_stats
                  WHERE hour > NOW() - INTERVAL '24 hours'
                  ORDER BY hour DESC
                  LIMIT 50;", conn); // Limit to top recent stats

            using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new MaterialStatsDto
                {
                    Hour = reader.GetDateTime(0),
                    MaterialId = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1),
                    MedianChange = reader.IsDBNull(2) ? 0 : reader.GetDouble(2),
                    AverageChange = reader.IsDBNull(3) ? 0 : reader.GetDouble(3),
                    StdDevChange = reader.IsDBNull(4) ? 0 : reader.GetDouble(4)
                });
            }
        }
        catch (PostgresException ex)
        {
            // Log or handle if toolkit is not installed/enabled, fallback or return empty
            // For now, simpler error handling:
            Console.WriteLine($"Error fetching stats: {ex.Message}");
        }

        return result;
    }
}

public class MaterialStatsDto
{
    public DateTime Hour { get; set; }
    public string MaterialId { get; set; } = string.Empty;
    public double MedianChange { get; set; }
    public double AverageChange { get; set; }
    public double StdDevChange { get; set; }
}
