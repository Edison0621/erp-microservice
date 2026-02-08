using Npgsql;
using System.Data;

namespace ErpSystem.Analytics.Infrastructure;

/// <summary>
/// Service to extract historical data from TimescaleDB for AI features
/// </summary>
public class TimescaleDataExtractor
{
    private readonly string _connectionString;

    public TimescaleDataExtractor(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AnalyticsConnection") ?? throw new ArgumentNullException("AnalyticsConnection");
    }

    /// <summary>
    /// Extracts aggregated daily inventory movements for a specific material
    /// </summary>
    public async Task<List<TimeSeriesData>> GetDailyInventoryMovements(string materialId, int pastDays)
    {
        var result = new List<TimeSeriesData>();

        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // Query the inventory_transactions_ts hypertable
        using var cmd = new NpgsqlCommand(
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

        using var reader = await cmd.ExecuteReaderAsync();
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
        var result = new List<TimeSeriesData>();

        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // Query cost movements or a dedicated financial transaction hypertable
        using var cmd = new NpgsqlCommand(
            @"SELECT 
                time_bucket('1 day', time) AS bucket,
                SUM(total_value) AS daily_value
              FROM cost_movements_ts
              WHERE time > NOW() - @pastDays * INTERVAL '1 day'
              GROUP BY bucket
              ORDER BY bucket ASC;", conn);

        cmd.Parameters.AddWithValue("pastDays", pastDays);

        using var reader = await cmd.ExecuteReaderAsync();
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
}
