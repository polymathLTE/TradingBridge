namespace TradingBridge.MarketData.Alerting
{
    public record AlertEntity
    {
        public string Id { get; init; } = System.Guid.NewGuid().ToString("N");
        public string Type { get; init; } = string.Empty;
        public string Symbol { get; init; } = string.Empty;
        public string Exchange { get; init; } = string.Empty;
        public int Severity { get; init; }
        public string PayloadJson { get; init; } = string.Empty;
        public System.DateTime CreatedAt { get; init; } = System.DateTime.UtcNow;
    }
}
