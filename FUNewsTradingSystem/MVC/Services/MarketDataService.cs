namespace FUNewsTradingSystem_MVC.Services;

/// <summary>
/// Interface for the shared market data singleton that collects live price history
/// from the MarketDataBackgroundService and serves it to API endpoints and SignalR clients.
/// </summary>
public interface IMarketDataService
{
    /// <summary>
    /// Current snapshot of all tracked symbols with their latest price and change data.
    /// </summary>
    IReadOnlyDictionary<string, MarketTick> CurrentPrices { get; }

    /// <summary>
    /// Rolling 1-hour price history per symbol (keyed by symbol, value = list of (timestamp, price) sorted ascending).
    /// </summary>
    IReadOnlyDictionary<string, List<(DateTime Time, double Price)>> PriceHistory { get; }

    /// <summary>
    /// All tracked symbol names.
    /// </summary>
    IReadOnlyCollection<string> TrackedSymbols { get; }

    /// <summary>
    /// Registers or updates the latest tick for a symbol, appending to the rolling history.
    /// Called by MarketDataBackgroundService after each successful fetch cycle.
    /// </summary>
    void UpdateTick(string symbol, double price, double change, double changePercent);

    /// <summary>
    /// Gets the rolling price history for a specific symbol, up to the last N data points.
    /// </summary>
    List<(DateTime Time, double Price)> GetHistory(string symbol, int maxPoints = 60);

    /// <summary>
    /// Returns true if the service has received at least one real data update for the given symbol.
    /// </summary>
    bool HasData(string symbol);
}

/// <summary>
/// A snapshot of a single market tick for one symbol.
/// </summary>
public class MarketTick
{
    public string Symbol { get; set; } = "";
    public double Price { get; set; }
    public double Change { get; set; }
    public double ChangePercent { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Thread-safe singleton that maintains live price data collected by the background service.
/// It stores the last 1 hour of 3-second tick data per symbol, giving ~1200 data points of history.
/// </summary>
public class MarketDataService : IMarketDataService
{
    private readonly Dictionary<string, MarketTick> _currentPrices = new();
    private readonly Dictionary<string, List<(DateTime Time, double Price)>> _history = new();
    private readonly HashSet<string> _symbols = new();
    private readonly object _lock = new();

    private const int MaxHistoryPoints = 1200; // ~1 hour at 3-second intervals

    public IReadOnlyDictionary<string, MarketTick> CurrentPrices
    {
        get
        {
            lock (_lock) { return new Dictionary<string, MarketTick>(_currentPrices); }
        }
    }

    public IReadOnlyDictionary<string, List<(DateTime Time, double Price)>> PriceHistory
    {
        get
        {
            lock (_lock)
            {
                var snapshot = new Dictionary<string, List<(DateTime Time, double Price)>>();
                foreach (var kvp in _history)
                {
                    snapshot[kvp.Key] = new List<(DateTime, double)>(kvp.Value);
                }
                return snapshot;
            }
        }
    }

    public IReadOnlyCollection<string> TrackedSymbols
    {
        get
        {
            lock (_lock) { return new HashSet<string>(_symbols).ToList().AsReadOnly(); }
        }
    }

    public void UpdateTick(string symbol, double price, double change, double changePercent)
    {
        var tick = new MarketTick
        {
            Symbol = symbol,
            Price = price,
            Change = change,
            ChangePercent = changePercent,
            Timestamp = DateTime.UtcNow
        };

        lock (_lock)
        {
            _currentPrices[symbol] = tick;

            if (!_history.TryGetValue(symbol, out var historyList))
            {
                historyList = new List<(DateTime, double)>();
                _history[symbol] = historyList;
                _symbols.Add(symbol);
            }

            historyList.Add((tick.Timestamp, price));

            // Trim history to keep last MaxHistoryPoints entries
            if (historyList.Count > MaxHistoryPoints)
            {
                historyList.RemoveRange(0, historyList.Count - MaxHistoryPoints);
            }
        }
    }

    public List<(DateTime Time, double Price)> GetHistory(string symbol, int maxPoints = 60)
    {
        lock (_lock)
        {
            if (!_history.TryGetValue(symbol, out var list) || list.Count == 0)
                return new List<(DateTime, double)>();

            // Return the last `maxPoints` entries
            var start = Math.Max(0, list.Count - maxPoints);
            return list.Skip(start).Take(maxPoints).ToList();
        }
    }

    public bool HasData(string symbol)
    {
        lock (_lock) { return _history.ContainsKey(symbol) && _history[symbol].Count > 0; }
    }
}
