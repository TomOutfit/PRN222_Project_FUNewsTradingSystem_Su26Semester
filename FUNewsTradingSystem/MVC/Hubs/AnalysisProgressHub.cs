using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FUNewsTradingSystem_MVC.Hubs
{
    /// <summary>
    /// SignalR Hub for broadcasting live news-synthesis progress details to specific Staff clients.
    /// </summary>
    public class AnalysisProgressHub : Hub
    {
        private readonly ILogger<AnalysisProgressHub> _logger;

        public AnalysisProgressHub(ILogger<AnalysisProgressHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Logs when a connection is established with a staff browser.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Staff connection established: ConnectionID={ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Logs when a client connection drops or finishes.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
            {
                _logger.LogWarning(exception, "Staff connection closed with exception: ConnectionID={ConnectionId}", Context.ConnectionId);
            }
            else
            {
                _logger.LogInformation("Staff connection closed cleanly: ConnectionID={ConnectionId}", Context.ConnectionId);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
