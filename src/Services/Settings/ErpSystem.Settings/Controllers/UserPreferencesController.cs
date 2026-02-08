using ErpSystem.Settings.Domain;
using ErpSystem.Settings.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpSystem.Settings.Controllers;

[ApiController]
[Route("api/v1/settings/preferences")]
public class UserPreferencesController : ControllerBase
{
    private readonly SettingsDbContext _context;
    private readonly ILogger<UserPreferencesController> _logger;

    public UserPreferencesController(SettingsDbContext context, ILogger<UserPreferencesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<UserPreference>> GetPreferences([FromQuery] string userId = "default-user")
    {
        var preference = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (preference == null)
        {
            // Return default preferences
            preference = new UserPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TenantId = "default-tenant"
            };
        }

        return Ok(preference);
    }

    [HttpPut]
    public async Task<ActionResult<UserPreference>> UpdatePreferences([FromBody] UserPreference preference)
    {
        var existing = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == preference.UserId);

        if (existing == null)
        {
            preference.Id = Guid.NewGuid();
            preference.CreatedAt = DateTime.UtcNow;
            preference.UpdatedAt = DateTime.UtcNow;
            _context.UserPreferences.Add(preference);
        }
        else
        {
            existing.Language = preference.Language;
            existing.TimeZone = preference.TimeZone;
            existing.DateFormat = preference.DateFormat;
            existing.CurrencyFormat = preference.CurrencyFormat;
            existing.Theme = preference.Theme;
            existing.FullName = preference.FullName;
            existing.Email = preference.Email;
            existing.Phone = preference.Phone;
            existing.Department = preference.Department;
            existing.EmailNotifications = preference.EmailNotifications;
            existing.SystemNotifications = preference.SystemNotifications;
            existing.PushNotifications = preference.PushNotifications;
            existing.NotifyOnOrders = preference.NotifyOnOrders;
            existing.NotifyOnInventory = preference.NotifyOnInventory;
            existing.NotifyOnFinance = preference.NotifyOnFinance;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(existing ?? preference);
    }

    [HttpPost("reset")]
    public async Task<ActionResult<UserPreference>> ResetPreferences([FromQuery] string userId = "default-user")
    {
        var existing = await _context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (existing != null)
        {
            _context.UserPreferences.Remove(existing);
            await _context.SaveChangesAsync();
        }

        var defaultPreference = new UserPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = "default-tenant"
        };

        return Ok(defaultPreference);
    }
}
