using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FUNewsTradingSystem_MVC.Hubs
{
    /// <summary>
    /// SignalR Hub for broadcasting live financial reports to public visitors.
    /// </summary>
    public class ReportHub : Hub
    {
        private readonly ILogger<ReportHub> _logger;

        public ReportHub(ILogger<ReportHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Logs when a connection is established with a visitor browser.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Visitor connection established: ConnectionID={ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Logs when a connection drops or finishes.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
            {
                _logger.LogWarning(exception, "Visitor connection closed with exception: ConnectionID={ConnectionId}", Context.ConnectionId);
            }
            else
            {
                _logger.LogInformation("Visitor connection closed cleanly: ConnectionID={ConnectionId}", Context.ConnectionId);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
