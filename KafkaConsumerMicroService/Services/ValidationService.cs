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
        private readonly IRecordValidator _validator;
        private readonly IRecordDispatcher _dispatcher;

        public ValidationService(IRecordValidator validator, IRecordDispatcher dispatcher)
        {
            _validator = validator;
            _dispatcher = dispatcher;
        }

        // Accepts raw message payload, validates and forwards to dispatcher
        public async Task ProcessMessageAsync(string rawPayload)
        {
            if (string.IsNullOrWhiteSpace(rawPayload)) return;

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

                rec.IsValid = _validator.IsValid(rec);
                await _dispatcher.StoreAndBroadcastAsync(rec);
            }
            catch (Exception ex)
            {
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
                await _dispatcher.StoreAndBroadcastAsync(err);
            }
        }
    }
}
