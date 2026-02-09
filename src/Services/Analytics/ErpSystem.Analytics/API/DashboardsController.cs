using Microsoft.AspNetCore.Mvc;
using ErpSystem.Analytics.Infrastructure;

namespace ErpSystem.Analytics.API;

[ApiController]
[Route("api/v1/[controller]")]
public class DashboardsController(BiAnalyticsService biService) : ControllerBase
{
    [HttpGet("inventory-turnover")]
    public async Task<IActionResult> GetInventoryTurnover([FromQuery] int days = 30)
    {
        string tenantId = "default-tenant"; // Should come from context/header
        List<InventoryTurnoverResult> result = await biService.GetInventoryTurnover(tenantId, days);
        return this.Ok(result);
    }

    [HttpGet("oee")]
    public async Task<IActionResult> GetOee()
    {
        string tenantId = "default-tenant";
        List<OeeResult> result = await biService.GetOeeDashboard(tenantId);
        return this.Ok(result);
    }
}
