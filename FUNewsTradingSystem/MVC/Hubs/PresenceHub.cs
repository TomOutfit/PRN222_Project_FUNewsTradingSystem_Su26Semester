using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace FUNewsTradingSystem_MVC.Hubs
{
    /// <summary>
    /// Tracks active visitor connections and broadcasts count globally.
    /// Disconnect broadcasts are debounced by 1.5 s to prevent transient spikes
    /// caused by full-page navigations (new connection fires before old one closes).
    /// </summary>
    public class PresenceHub : Hub
    {
        private static readonly ConcurrentDictionary<string, byte> ConnectedUsers = new();

        // Debounce: cancel the pending broadcast if another connect/disconnect fires
        private static CancellationTokenSource _broadcastCts = new();
        private static readonly object _broadcastLock = new();

        private readonly ILogger<PresenceHub> _logger;
        private readonly IHubContext<PresenceHub> _ctx;

        public PresenceHub(ILogger<PresenceHub> logger, IHubContext<PresenceHub> ctx)
        {
            _logger = logger;
            _ctx = ctx;
        }

        public override async Task OnConnectedAsync()
        {
            ConnectedUsers.TryAdd(Context.ConnectionId, 0);
            _logger.LogInformation("Presence +connected: {ConnectionId}. Raw count: {Count}", Context.ConnectionId, ConnectedUsers.Count);

            // Connect always broadcasts immediately so the joining client sees the right count
            ScheduleBroadcast(_ctx, delay: TimeSpan.Zero);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            ConnectedUsers.TryRemove(Context.ConnectionId, out _);
            _logger.LogInformation("Presence -disconnected: {ConnectionId}. Raw count: {Count}", Context.ConnectionId, ConnectedUsers.Count);

            // Debounce: wait 1.5 s before broadcasting — gives the replacement connection
            // time to register so we never send an inflated count to existing clients.
            ScheduleBroadcast(_ctx, delay: TimeSpan.FromMilliseconds(1500));

            await base.OnDisconnectedAsync(exception);
        }

        private static void ScheduleBroadcast(IHubContext<PresenceHub> ctx, TimeSpan delay)
        {
            CancellationTokenSource newCts;
            CancellationTokenSource oldCts;

            lock (_broadcastLock)
            {
                oldCts = _broadcastCts;
                newCts = new CancellationTokenSource();
                _broadcastCts = newCts;
            }

            // Cancel the previous pending broadcast
            oldCts.Cancel();
            oldCts.Dispose();

            var token = newCts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    if (delay > TimeSpan.Zero)
                        await Task.Delay(delay, token);

                    if (!token.IsCancellationRequested)
                        await ctx.Clients.All.SendAsync("UpdateOnlineCount", ConnectedUsers.Count, token);
                }
                catch (OperationCanceledException) { /* superseded — normal */ }
            }, CancellationToken.None);
        }
    }
}
