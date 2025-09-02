public interface IExchangeMarketDataClient : IAsyncDisposable
{
    string ExchangeName { get; }
    /// Subscribe produces an async stream of market events for requested symbols.
    IAsyncEnumerable<MarketEvent> SubscribeAsync(IEnumerable<string> symbols, CancellationToken ct);
    /// REST confirmation fetch for top-of-book (optional but recommended)
    Task<OrderBookUpdate?> FetchOrderBookSnapshotAsync(string symbol, int depth = 5, CancellationToken ct = default);
}

