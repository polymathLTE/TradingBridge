using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

public class PriceAggregator
{
    // symbol -> exchange -> snapshot
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, OrderBookUpdate>> _store = new();

    public void Apply(OrderBookUpdate update)
    {
        var exchMap = _store.GetOrAdd(update.Symbol, _ => new ConcurrentDictionary<string, OrderBookUpdate>());
        exchMap[update.Exchange] = update;
    }

    public IReadOnlyDictionary<string, OrderBookUpdate> SnapshotsForSymbol(string symbol)
    {
        return _store.TryGetValue(symbol, out var dict) ? dict.ToDictionary(kv => kv.Key, kv => kv.Value) : new Dictionary<string, OrderBookUpdate>();
    }

    public (string bestBidExch, decimal bestBid, string bestAskExch, decimal bestAsk) GetBestPrices(string symbol)
    {
        var snaps = SnapshotsForSymbol(symbol);
        decimal bestBid = decimal.MinValue; string bestBidExch = "";
        decimal bestAsk = decimal.MaxValue; string bestAskExch = "";
        foreach (var snap in snaps.Values)
        {
            var topBid = snap.Bids?.FirstOrDefault()?.Price ?? decimal.MinValue;
            var topAsk = snap.Asks?.FirstOrDefault()?.Price ?? decimal.MaxValue;
            if (topBid > bestBid) { bestBid = topBid; bestBidExch = snap.Exchange; }
            if (topAsk < bestAsk) { bestAsk = topAsk; bestAskExch = snap.Exchange; }
        }
        return (bestBidExch, bestBid, bestAskExch, bestAsk);
    }
}

