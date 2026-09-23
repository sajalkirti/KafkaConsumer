using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ValidationService> _logger;

        public ValidationService(IRecordValidator validator, IRecordDispatcher dispatcher, ILogger<ValidationService> logger)
        {
            _validator = validator;
            _dispatcher = dispatcher;
            _logger = logger;
        }

        // Accepts raw message payload, validates and forwards to dispatcher
        public async Task ProcessMessageAsync(string rawPayload)
        {
            if (string.IsNullOrWhiteSpace(rawPayload)) return;

            try
            {
                // First attempt: parse as-is
                using var doc = JsonDocument.Parse(rawPayload);
                await ProcessJsonDocumentAsync(doc, rawPayload).ConfigureAwait(false);
            }
            catch (JsonException jex)
            {
                _logger?.LogWarning(jex, "JSON parse failed, attempting lenient sanitization");

                // Try a lenient sanitization pass to fix common malformed numeric forms like ".34" or "-.34"
                var sanitized = SanitizeLeadingDecimalNumbers(rawPayload);
                try
                {
                    using var doc2 = JsonDocument.Parse(sanitized);
                    await ProcessJsonDocumentAsync(doc2, sanitized).ConfigureAwait(false);
                }
                catch (Exception ex2)
                {
                    _logger?.LogError(ex2, "Sanitized parse also failed; recording parse error");
                    var err = new MarketRecord
                    {
                        RawPayload = rawPayload,
                        SourceSystem = "<parse_error>",
                        AccountNumber = 0,
                        PnLAmount = 0,
                        IsValid = false,
                        Metadata = JsonSerializer.Serialize(new { error = ex2.Message, processedAt = DateTimeOffset.UtcNow }),
                        ReceivedAt = DateTimeOffset.UtcNow
                    };
                    await _dispatcher.StoreAndBroadcastAsync(err).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error in ProcessMessageAsync");
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
                await _dispatcher.StoreAndBroadcastAsync(err).ConfigureAwait(false);
            }

        }

        private async Task ProcessJsonDocumentAsync(JsonDocument doc, string rawPayload)
        {
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
            await _dispatcher.StoreAndBroadcastAsync(rec).ConfigureAwait(false);
        }

        private static string SanitizeLeadingDecimalNumbers(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return json;

            // Patterns to fix occurrences like : .34  or :-.34 -> add leading zero
            // Match colon, optional whitespace, optional minus, dot, digits
            // Replace with colon, space, optional minus, 0.<digits>
            // Use a regex with a capture for the sign and the digits.
            var pattern = @"(:\s*)(-?)(\.(\d+))";
            var repl = "$1$2 0.$4";
            try
            {
                return Regex.Replace(json, pattern, repl);
            }
            catch
            {
                return json; // if regex fails, return original
            }
        }
    }
}
