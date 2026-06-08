using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace OptionsEdge.API.Infrastructure.SignalR;

public class MarketHub : Hub
{
    public async Task SubscribeToSymbol(string symbol)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, symbol.ToUpper());
    }

    public async Task UnsubscribeFromSymbol(string symbol)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, symbol.ToUpper());
    }

    public async Task SubscribeToAlerts(string? userId = null)
    {
        var currentUserId = GetCurrentUserId();
        if (!string.IsNullOrWhiteSpace(userId) &&
            !string.Equals(userId, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new HubException("Cannot subscribe to another user's alerts.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"alerts:{currentUserId}");
    }

    public async Task UnsubscribeFromAlerts(string? userId = null)
    {
        var currentUserId = GetCurrentUserId();
        if (!string.IsNullOrWhiteSpace(userId) &&
            !string.Equals(userId, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new HubException("Cannot unsubscribe from another user's alerts.");
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"alerts:{currentUserId}");
    }

    private string GetCurrentUserId()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            throw new HubException("Authenticated user id is missing.");

        return userId;
    }
}
