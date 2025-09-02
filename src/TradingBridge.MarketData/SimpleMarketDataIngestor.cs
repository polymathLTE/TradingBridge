using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;


public class SimpleMarketDataIngestor : IHostedService
{
    private readonly IEnumerable<IExchangeMarketDataClient> _clients;
    private readonly Channel<MarketEvent> _outChannel;
    private readonly ILogger<SimpleMarketDataIngestor> _log;
    private CancellationTokenSource? _cts;
    private List<Task>? _readerTasks;

    public ChannelReader<MarketEvent> Reader => _outChannel.Reader;

    public SimpleMarketDataIngestor(IEnumerable<IExchangeMarketDataClient> clients, ILogger<SimpleMarketDataIngestor> log)
    {
        _clients = clients;
        _log = log;
        _outChannel = Channel.CreateUnbounded<MarketEvent>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _readerTasks = _clients.Select(c => Task.Run(async () =>
        {
            await foreach (var ev in c.SubscribeAsync(Array.Empty<string>(), _cts.Token))
            {
                await _outChannel.Writer.WriteAsync(ev, _cts.Token);
            }
        }, _cts.Token)).ToList();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        if (_readerTasks != null) await Task.WhenAll(_readerTasks);
        _outChannel.Writer.Complete();
    }
}

