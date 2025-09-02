using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;

public class ArbDetector : BackgroundService
{
    private readonly ChannelReader<MarketEvent> _marketReader;
    private readonly PriceAggregator _aggregator;
    private readonly ChannelWriter<ArbCandidate> _arbWriter;
    private readonly ILogger<ArbDetector> _log;
    private readonly decimal _minNetPct; // e.g. 0.005m = 0.5% net

    public ArbDetector(SimpleMarketDataIngestor ingestor, PriceAggregator aggregator, Channel<ArbCandidate> arbChannel, ILogger<ArbDetector> log, decimal minNetPct = 0.005m)
    {
        _marketReader = ingestor.Reader;
        _aggregator = aggregator;
        _arbWriter = arbChannel.Writer;
        _log = log;
        _minNetPct = minNetPct;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var ev in _marketReader.ReadAllAsync(stoppingToken))
        {
            if (ev is OrderBookUpdate ob)
            {
                _aggregator.Apply(ob);

                // quick check only when we get updates
                var (bestBidExch, bestBid, bestAskExch, bestAsk) = _aggregator.GetBestPrices(ob.Symbol);
                if (bestBidExch != bestAskExch && bestAsk > 0 && bestBid > 0)
                {
                    var grossSpread = bestBid - bestAsk;
                    var netPct = grossSpread / bestAsk;
                    if (netPct >= _minNetPct)
                    {
                        var candidate = new ArbCandidate(ob.Symbol, bestAskExch, bestBidExch, bestAsk, bestBid, netPct, estimatedProfit: netPct /* placeholder */);
                        await _arbWriter.WriteAsync(candidate, stoppingToken);
                        _log.LogInformation("Arb candidate: {Candidate}", candidate);
                    }
                }
            }
        }
    }
}

public record ArbCandidate(string Symbol, string ExchangeBuy, string ExchangeSell, decimal BuyPrice, decimal SellPrice, decimal NetSpreadPct, decimal EstimatedProfit);

