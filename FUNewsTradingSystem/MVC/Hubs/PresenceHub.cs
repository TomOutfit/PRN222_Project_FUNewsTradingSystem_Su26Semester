using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace FUNewsTradingSystem_MVC.Hubs
{
    /// <summary>
    /// SignalR Hub for keeping track of active connected visitor sessions and broadcasting the count globally.
    /// </summary>
    public class PresenceHub : Hub
    {
        private static readonly ConcurrentDictionary<string, byte> ConnectedUsers = new ConcurrentDictionary<string, byte>();
        private readonly ILogger<PresenceHub> _logger;

        public PresenceHub(ILogger<PresenceHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            ConnectedUsers.TryAdd(Context.ConnectionId, 0);
            _logger.LogInformation("Presence connection added: {ConnectionId}. Total online count: {Count}", Context.ConnectionId, ConnectedUsers.Count);
            
            // Broadcast the updated online count to all clients
            await Clients.All.SendAsync("UpdateOnlineCount", ConnectedUsers.Count);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            ConnectedUsers.TryRemove(Context.ConnectionId, out _);
            _logger.LogInformation("Presence connection removed: {ConnectionId}. Total online count: {Count}", Context.ConnectionId, ConnectedUsers.Count);
            
            // Broadcast the updated online count to all clients
            await Clients.All.SendAsync("UpdateOnlineCount", ConnectedUsers.Count);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
