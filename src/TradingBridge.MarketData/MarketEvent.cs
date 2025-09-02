public abstract record MarketEvent(string Exchange, string Symbol, long TimestampMs);
public record TradeTick(string Exchange, string Symbol, decimal Price, decimal Quantity, string Side, long TimestampMs)
    : MarketEvent(Exchange, Symbol, TimestampMs);
public record OrderBookLevel(decimal Price, decimal Quantity);
public record OrderBookUpdate(string Exchange, string Symbol, IReadOnlyList<OrderBookLevel> Bids, IReadOnlyList<OrderBookLevel> Asks, long TimestampMs)
    : MarketEvent(Exchange, Symbol, TimestampMs);

