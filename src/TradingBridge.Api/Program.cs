using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TradingBridge.Core;
using TradingBridge.MarketData;
using TradingBridge.Services; // SignalChannel, TradeProcessor, ITradeExecutor, MockTradeExecutor

var builder = WebApplication.CreateBuilder(args);

// ---- Logging ----
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// ---- DI registrations ----
// Core processing queue (in-memory Channel)
builder.Services.AddSingleton<SignalChannel>(); // used by webhook -> SignalProcessor

// Trade executor abstraction: start with mock for MVP
builder.Services.AddSingleton<ITradeExecutor, MockTradeExecutor>();

// Register the background signal processor which consumes SignalChannel
builder.Services.AddHostedService<TradeProcessor>();

// MarketData components:
// - A channel to publish ArbCandidate events so API/WebSocket can stream them
builder.Services.AddSingleton(_ => Channel.CreateUnbounded<ArbCandidate>());

// - PriceAggregator holds in-memory top-of-book snapshots
builder.Services.AddSingleton<PriceAggregator>();

// - Register market-data clients (mock or real). For MVP, register a mock replay client.
//   Add one registration per exchange:
builder.Services.AddSingleton<IExchangeMarketDataClient, MockExchangeMarketDataClient>(); // replace/add real clients later

// - MarketData ingestor (hosted) reads from all registered IExchangeMarketDataClient
builder.Services.AddHostedService<SimpleMarketDataIngestor>();

// - Arb detector (hosted) consumes market events via ingestor's channel and writes ArbCandidates to arbChannel
builder.Services.AddHostedService<ArbDetector>();

// ---- Controllers / minimal API ----
var app = builder.Build();

// Simple health endpoint
app.MapGet("/", () => Results.Ok(new { status = "ok" }));

// Webhook endpoint: accept TradingView-like JSON and enqueue a TradingSignal
app.MapPost("/api/webhook", async (HttpRequest request, SignalChannel channel, ILogger<Program> logger) =>
{
    using var doc = await System.Text.Json.JsonDocument.ParseAsync(request.Body);
    var signal = TradingSignal.FromJson(doc.RootElement);
    await channel.Writer.WriteAsync(signal);
    logger.LogInformation("Enqueued signal {Id} {Symbol} {Action}", signal.Id, signal.Symbol, signal.Action);
    return Results.Accepted(new { id = signal.Id });
});

// Simple endpoint to read latest arb candidates from the arb channel (non-destructive peek for demo)
app.MapGet("/api/market/arbs", async (Channel<ArbCandidate> arbChannel) =>
{
    var items = new List<ArbCandidate>();
    var reader = arbChannel.Reader;
    while (reader.TryRead(out var c) && items.Count < 50) items.Add(c); // destructive read for sample; persist in DB in real app
    return Results.Ok(items);
});

app.Run();

