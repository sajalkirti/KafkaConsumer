using System.Text.Json;
using System.Threading.Tasks;
using KafkaConsumerMicroService.Data;
using KafkaConsumerMicroService.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace KafkaConsumerMicroService.Services
{
    public class RecordDispatcher : IRecordDispatcher
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<DataHub> _hub;

        public RecordDispatcher(AppDbContext db, IHubContext<DataHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        public async Task StoreAndBroadcastAsync(MarketRecord record)
        {
            if (record == null) return;

            // Ensure metadata contains processed timestamp
            if (string.IsNullOrWhiteSpace(record.Metadata))
            {
                record.Metadata = JsonSerializer.Serialize(new { processedAt = System.DateTimeOffset.UtcNow, source = record.SourceSystem });
            }

            _db.MarketRecords.Add(record);
            await _db.SaveChangesAsync();

            var method = record.IsValid ? "ValidRecord" : "InvalidRecord";
            await _hub.Clients.All.SendAsync(method, record);
        }
    }
}
