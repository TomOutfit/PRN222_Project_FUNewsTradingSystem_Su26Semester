using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FUNewsTradingSystem_MVC.Hubs
{
    /// <summary>
    /// SignalR Hub for streaming real-time simulated stock prices and market index ticks.
    /// </summary>
    public class MarketTickerHub : Hub
    {
        private readonly ILogger<MarketTickerHub> _logger;

        public MarketTickerHub(ILogger<MarketTickerHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Market ticker subscription active for client: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Market ticker subscription removed for client: {ConnectionId}", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
