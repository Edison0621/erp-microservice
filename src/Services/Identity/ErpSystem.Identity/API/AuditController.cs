using Microsoft.AspNetCore.Mvc;
using ErpSystem.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Identity.API.Controllers;

[ApiController]
[Route("api/v1/identity/audit-logs")]
public class AuditController(IdentityReadDbContext readContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string? eventType)
    {
        IQueryable<AuditLogEntry> query = readContext.AuditLogs.AsNoTracking().AsQueryable();

        if (fromDate.HasValue) query = query.Where(x => x.Timestamp >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(x => x.Timestamp <= toDate.Value);
        if (!string.IsNullOrEmpty(eventType)) query = query.Where(x => x.EventType == eventType);

        query = query.OrderByDescending(x => x.Timestamp).Take(100); // Limit

        return this.Ok(await query.ToListAsync());
    }
}
