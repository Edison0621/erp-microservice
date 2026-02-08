namespace ErpSystem.Settings.Domain;

public class UserPreference
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    
    // Display Preferences
    public string Language { get; set; } = "en";
    public string TimeZone { get; set; } = "UTC+8";
    public string DateFormat { get; set; } = "YYYY-MM-DD";
    public string CurrencyFormat { get; set; } = "USD";
    public string Theme { get; set; } = "light";
    
    // Profile
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    
    // Notifications
    public bool EmailNotifications { get; set; } = true;
    public bool SystemNotifications { get; set; } = true;
    public bool PushNotifications { get; set; } = false;
    public bool NotifyOnOrders { get; set; } = true;
    public bool NotifyOnInventory { get; set; } = true;
    public bool NotifyOnFinance { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
