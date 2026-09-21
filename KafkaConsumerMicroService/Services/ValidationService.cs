using System;
using System.Text.Json;
using System.Threading.Tasks;
using KafkaConsumerMicroService.Data;
using KafkaConsumerMicroService.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace KafkaConsumerMicroService.Services
{
    public class ValidationService : IValidationService
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<DataHub> _hub;

        public ValidationService(AppDbContext db, IHubContext<DataHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        // Accepts raw message payload, validates against business rules, persists and broadcasts
        public async Task ProcessMessageAsync(string rawPayload)
        {
            if (string.IsNullOrWhiteSpace(rawPayload)) return;

            // Expecting JSON with SourceSystem (string), AccountNumber (int), PnLAmount (double)
            try
            {
                using var doc = JsonDocument.Parse(rawPayload);
                var root = doc.RootElement;

                var rec = new MarketRecord
                {
                    RawPayload = rawPayload,
                    SourceSystem = root.GetProperty("SourceSystem").GetString(),
                    AccountNumber = root.GetProperty("AccountNumber").GetInt64(),
                    PnLAmount = root.GetProperty("PnLAmount").GetDouble(),
                    ReceivedAt = DateTimeOffset.UtcNow
                };

                // Business rule: PnL amount zero => invalid
                rec.IsValid = Math.Abs(rec.PnLAmount) > double.Epsilon;

                // Metadata example (can be expanded later)
                rec.Metadata = JsonSerializer.Serialize(new { processedAt = DateTimeOffset.UtcNow, source = rec.SourceSystem });

                _db.MarketRecords.Add(rec);
                await _db.SaveChangesAsync();

                // Broadcast to connected clients in real-time
                var method = rec.IsValid ? "ValidRecord" : "InvalidRecord";
                await _hub.Clients.All.SendAsync(method, rec);
            }
            catch (Exception ex)
            {
                // In production, use structured logging and dead-lettering. For now, persist minimal audit.
                var err = new MarketRecord
                {
                    RawPayload = rawPayload,
                    SourceSystem = "<parse_error>",
                    AccountNumber = 0,
                    PnLAmount = 0,
                    IsValid = false,
                    Metadata = JsonSerializer.Serialize(new { error = ex.Message, processedAt = DateTimeOffset.UtcNow }),
                    ReceivedAt = DateTimeOffset.UtcNow
                };
                _db.MarketRecords.Add(err);
                await _db.SaveChangesAsync();
                await _hub.Clients.All.SendAsync("InvalidRecord", err);
            }
        }
    }
}
