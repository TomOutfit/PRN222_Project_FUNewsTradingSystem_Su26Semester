using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FUNewsTradingSystem_MVC.Hubs;

namespace FUNewsTradingSystem_MVC.Services
{
    /// <summary>
    /// Hosted Background Service that simulates live market stock prices and index changes,
    /// broadcasting them every 3 seconds to all clients subscribed to the MarketTickerHub.
    /// </summary>
    public class MarketDataBackgroundService : BackgroundService
    {
        private readonly IHubContext<MarketTickerHub> _hubContext;
        private readonly ILogger<MarketDataBackgroundService> _logger;
        private readonly Random _random = new Random();

        // Baseline stock prices for live simulation
        private readonly Dictionary<string, double> _prices = new Dictionary<string, double>
        {
            { "AAPL", 182.30 },
            { "MSFT", 421.90 },
            { "NVDA", 894.50 },
            { "BTC", 67450.00 },
            { "FNTS", 1520.00 } // FNTS Sentiment Index
        };

        public MarketDataBackgroundService(
            IHubContext<MarketTickerHub> hubContext,
            ILogger<MarketDataBackgroundService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MarketDataBackgroundService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var ticks = new List<object>();
                    var keys = new List<string>(_prices.Keys);

                    foreach (var symbol in keys)
                    {
                        double currentPrice = _prices[symbol];
                        
                        // Random walk model: fluctuation between -0.15% and +0.18%
                        double changePercent = (_random.NextDouble() * 0.33) - 0.15;
                        double change = currentPrice * (changePercent / 100.0);
                        double newPrice = Math.Round(currentPrice + change, 2);

                        if (symbol == "BTC")
                        {
                            newPrice = Math.Round(newPrice, 0);
                        }

                        _prices[symbol] = newPrice;

                        ticks.Add(new
                        {
                            symbol = symbol,
                            price = newPrice,
                            change = Math.Round(change, 2),
                            changePercent = Math.Round(changePercent, 2),
                            timestamp = DateTime.UtcNow.ToString("o")
                        });
                    }

                    // Broadcast the tick updates payload to all clients
                    await _hubContext.Clients.All.SendAsync("ReceiveMarketTicks", ticks, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error encountered while generating/broadcasting simulated market ticks.");
                }

                // Tick interval: 3 seconds
                await Task.Delay(3000, stoppingToken);
            }

            _logger.LogInformation("MarketDataBackgroundService is stopping.");
        }
    }
}
