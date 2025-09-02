using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TradingBridge.MarketData.Alerting
{
    // Very small in-memory repo for MVP. Replace with EF Core later.
    public class AlertRepository
    {
        private readonly ConcurrentQueue<AlertEntity> _queue = new();

        public Task AddAsync(AlertEntity alert)
        {
            _queue.Enqueue(alert);
            return Task.CompletedTask;
        }

        public Task<List<AlertEntity>> GetLatestAsync(int take = 50)
        {
            return Task.FromResult(_queue.Reverse().Take(take).ToList());
        }
    }
}
