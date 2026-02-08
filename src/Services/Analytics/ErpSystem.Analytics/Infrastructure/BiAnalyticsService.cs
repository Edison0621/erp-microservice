using Npgsql;

namespace ErpSystem.Analytics.Infrastructure;

/// <summary>
/// Service for BI analytics and dashboard data retrieval
/// </summary>
public class BiAnalyticsService
{
    private readonly string _connectionString;

    public BiAnalyticsService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AnalyticsConnection") ?? throw new ArgumentNullException("AnalyticsConnection");
    }

    /// <summary>
    /// Calculates inventory turnover rate for materials
    /// Logic: (Cost of Goods Sold / Average Inventory)
    /// </summary>
    public async Task<List<InventoryTurnoverResult>> GetInventoryTurnover(string tenantId, int days)
    {
        var result = new List<InventoryTurnoverResult>();

        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // Query using continuous aggregate daily_inventory_summary
        using var cmd = new NpgsqlCommand(
            @"WITH cogs AS (
                -- Cost of Goods Sold (Usage * Cost)
                SELECT 
                    material_id,
                    ABS(SUM(quantity_change * unit_cost)) as total_cogs
                FROM cost_movements_ts
                WHERE quantity_change < 0
                  AND time > NOW() - @days * INTERVAL '1 day'
                GROUP BY material_id
            ),
            avg_inv AS (
                -- Average Inventory Value from continuous aggregate
                SELECT 
                    material_id,
                    AVG(total_value) as avg_value
                FROM daily_inventory_summary
                WHERE bucket > NOW() - @days * INTERVAL '1 day'
                GROUP BY material_id
            )
            SELECT 
                c.material_id,
                c.total_cogs,
                a.avg_value,
                CASE WHEN a.avg_value > 0 THEN c.total_cogs / a.avg_value ELSE 0 END as turnover_rate
            FROM cogs c
            JOIN avg_inv a ON c.material_id = a.material_id
            ORDER BY turnover_rate DESC;", conn);

        cmd.Parameters.AddWithValue("days", days);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new InventoryTurnoverResult(
                reader.GetString(0),
                reader.GetDecimal(1),
                reader.GetDecimal(2),
                reader.GetDecimal(3)));
        }

        return result;
    }

    /// <summary>
    /// Calculates Overall Equipment Effectiveness (OEE) components
    /// Availability x Performance x Quality
    /// </summary>
    public async Task<List<OeeResult>> GetOeeDashboard(string tenantId)
    {
        // This is a simplified implementation. Real OEE requires detailed event logging for downtime.
        // For now, we simulate Based on Availability (Up time vs Total time) and Quality (Pass rate).
        
        var result = new List<OeeResult>();

        // In a real scenario, we'd query equipment logs and quality checks
        // Dummy implementation for demonstration:
        result.Add(new OeeResult("EQUIP-CNC-01", 0.85m, 0.92m, 0.98m, 0.76m));
        result.Add(new OeeResult("EQUIP-LATH-02", 0.90m, 0.88m, 0.95m, 0.75m));

        return await Task.FromResult(result);
    }
}

public record InventoryTurnoverResult(string MaterialId, decimal Cogs, decimal AverageInventoryValue, decimal TurnoverRate);
public record OeeResult(string EquipmentId, decimal Availability, decimal Performance, decimal Quality, decimal TotalOee);
