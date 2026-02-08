using Microsoft.AspNetCore.Mvc;
using ErpSystem.Analytics.Infrastructure;

namespace ErpSystem.Analytics.API;

[ApiController]
[Route("api/v1/[controller]")]
public class DashboardsController : ControllerBase
{
    private readonly BiAnalyticsService _biService;

    public DashboardsController(BiAnalyticsService biService)
    {
        _biService = biService;
    }

    [HttpGet("inventory-turnover")]
    public async Task<IActionResult> GetInventoryTurnover([FromQuery] int days = 30)
    {
        var tenantId = "default-tenant"; // Should come from context/header
        var result = await _biService.GetInventoryTurnover(tenantId, days);
        return Ok(result);
    }

    [HttpGet("oee")]
    public async Task<IActionResult> GetOee()
    {
        var tenantId = "default-tenant";
        var result = await _biService.GetOeeDashboard(tenantId);
        return Ok(result);
    }
}
