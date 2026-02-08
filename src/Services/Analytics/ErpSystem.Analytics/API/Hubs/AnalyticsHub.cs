using Microsoft.AspNetCore.SignalR;

namespace ErpSystem.Analytics.API.Hubs;

public class AnalyticsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        // Optional: Send initial data or welcome message
    }
}
