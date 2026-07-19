using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using FUNewsTradingSystem_MVC.Hubs;
using FUNewsTradingSystem_BusinessLayer.Services.Interfaces;

namespace FUNewsTradingSystem_MVC.Services
{
    /// <summary>
    /// Hosted Background Service that fetches live market stock prices and index changes from Yahoo Finance,
    /// broadcasting them every 3 seconds to all clients subscribed to the MarketTickerHub.
    /// Falls back to simulation model if Yahoo Finance API is unavailable.
    /// </summary>
    public class MarketDataBackgroundService : BackgroundService
    {
        private readonly IHubContext<MarketTickerHub> _hubContext;
        private readonly ILogger<MarketDataBackgroundService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMarketDataService _marketDataService;
        private readonly Random _random = new Random();

        // Baseline stock prices for live simulation / current prices cache
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
            ILogger<MarketDataBackgroundService> logger,
            HttpClient httpClient,
            IServiceProvider serviceProvider,
            IMarketDataService marketDataService)
        {
            _hubContext = hubContext;
            _logger = logger;
            _httpClient = httpClient;
            _serviceProvider = serviceProvider;
            _marketDataService = marketDataService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MarketDataBackgroundService is starting with Yahoo Finance real-time data source.");

            while (!stoppingToken.IsCancellationRequested)
            {
                // Fetch dynamic tags from database
                var symbolsToFetch = new HashSet<string>(_prices.Keys);
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var tagService = scope.ServiceProvider.GetRequiredService<ITagService>();
                        var tags = await tagService.GetAllTagsAsync();
                        foreach (var tag in tags)
                        {
                            if (!string.IsNullOrWhiteSpace(tag.TagName))
                            {
                                string cleanSymbol = tag.TagName.Trim().ToUpper();
                                symbolsToFetch.Add(cleanSymbol);
                                if (!_prices.ContainsKey(cleanSymbol))
                                {
                                    _prices[cleanSymbol] = cleanSymbol switch
                                    {
                                        "BTC" => 67450.00,
                                        "FNTS" => 1520.00,
                                        _ => _random.Next(50, 600)
                                    };
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to retrieve tags from database. Using cached symbols.");
                }

                bool success = false;
                try
                {
                    // Map to Yahoo symbols
                    var yahooSymbols = symbolsToFetch.Select(MapToYahooSymbol).ToList();
                    var symbolsQuery = string.Join(",", yahooSymbols);

                    // Fetch real-time intraday data from Yahoo Finance chart API
                    // Using 5-minute interval for up-to-date price ticks during market hours
                    var request = new HttpRequestMessage(HttpMethod.Get,
                        $"https://query1.finance.yahoo.com/v8/finance/chart/{Uri.EscapeDataString(symbolsQuery)}?range=5d&interval=5m&includePrePost=false");
                    request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(3.5));

                    var response = await _httpClient.SendAsync(request, cts.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync(stoppingToken);
                        using var doc = JsonDocument.Parse(content);

                        if (doc.RootElement.TryGetProperty("chart", out var chartNode) &&
                            chartNode.TryGetProperty("result", out var resultList) &&
                            resultList.ValueKind == JsonValueKind.Array)
                        {
                            var ticks = new List<object>();
                            foreach (var result in resultList.EnumerateArray())
                            {
                                if (!result.TryGetProperty("meta", out var meta) ||
                                    !meta.TryGetProperty("symbol", out var symProp))
                                    continue;

                                string rawSymbol = symProp.GetString() ?? "";
                                string symbol = MapFromYahooSymbol(rawSymbol);

                                // Get latest close price from the last available bar
                                double price = 0;
                                if (result.TryGetProperty("indicators", out var indNode) &&
                                    indNode.TryGetProperty("quote", out var quoteList) &&
                                    quoteList.ValueKind == JsonValueKind.Array &&
                                    quoteList.GetArrayLength() > 0)
                                {
                                    var closeArr = quoteList[0];
                                    if (closeArr.TryGetProperty("close", out var closeProp) &&
                                        closeProp.ValueKind == JsonValueKind.Array)
                                    {
                                        var arrLen = closeProp.GetArrayLength();
                                        if (arrLen > 0)
                                        {
                                            // Walk backward to find last non-null price
                                            for (int idx = arrLen - 1; idx >= 0; idx--)
                                            {
                                                var elem = closeProp[idx];
                                                if (elem.ValueKind == JsonValueKind.Number)
                                                {
                                                    price = elem.GetDouble();
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }

                                if (price <= 0) continue;

                                // Calculate change from previous close
                                double prevClose = 0;
                                if (meta.TryGetProperty("previousClose", out var pcProp) ||
                                    meta.TryGetProperty("chartPreviousClose", out pcProp))
                                    prevClose = pcProp.GetDouble();
                                if (prevClose <= 0) prevClose = price * 0.999;

                                double change = Math.Round(price - prevClose, symbol == "BTC" ? 0 : 2);
                                double changePercent = Math.Round((change / prevClose) * 100, 2);

                                _prices[symbol] = price;

                                ticks.Add(new
                                {
                                    symbol = symbol,
                                    price = Math.Round(price, symbol == "BTC" ? 0 : 2),
                                    change = change,
                                    changePercent = changePercent,
                                    timestamp = DateTime.UtcNow.ToString("o")
                                });

                                // Record to MarketDataService for chart history
                                _marketDataService.UpdateTick(symbol, price, change, changePercent);
                            }

                            if (ticks.Count > 0)
                            {
                                await _hubContext.Clients.All.SendAsync("ReceiveMarketTicks", ticks, stoppingToken);
                                success = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to retrieve real-time market data from Yahoo Finance. Falling back to simulation model.");
                }

                if (!success)
                {
                    // FALLBACK: Generate simulated market ticks
                    try
                    {
                        var ticks = new List<object>();
                        // Record to MarketDataService for chart history
                        foreach (var symbol in symbolsToFetch)
                        {
                            double currentPrice = _prices[symbol];
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

                            _marketDataService.UpdateTick(symbol, newPrice, Math.Round(change, 2), Math.Round(changePercent, 2));
                        }

                        await _hubContext.Clients.All.SendAsync("ReceiveMarketTicks", ticks, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error encountered while generating/broadcasting simulated market ticks.");
                    }
                }

                // Tick interval: 3 seconds
                await Task.Delay(3000, stoppingToken);
            }

            _logger.LogInformation("MarketDataBackgroundService is stopping.");
        }

        private string MapToYahooSymbol(string symbol)
        {
            return symbol.ToUpper() switch
            {
                "BTC" => "BTC-USD",
                "FNTS" => "^IXIC",
                _ => symbol
            };
        }

        private string MapFromYahooSymbol(string rawSymbol)
        {
            return rawSymbol.ToUpper() switch
            {
                "BTC-USD" => "BTC",
                "^IXIC" => "FNTS",
                _ => rawSymbol
            };
        }
    }
}
