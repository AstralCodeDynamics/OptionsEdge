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

    public async Task SubscribeToAlerts(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"alerts:{userId}");
    }

    public async Task UnsubscribeFromAlerts(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"alerts:{userId}");
    }
}
