using System;

namespace KafkaConsumerMicroService.Data
{
    // Represents a processed PnL record. Designed to be extensible for high-throughput storage.
    public class MarketRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Source system that produced the message (e.g., upstream feed name)
        public string SourceSystem { get; set; }

        // Account number associated with the PnL
        public long AccountNumber { get; set; }

        // Reported PnL amount
        public double PnLAmount { get; set; }

        // Mark whether record passed validation
        public bool IsValid { get; set; }

        // The moment we received/processed the message
        public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;

        // Raw payload payload (kept for audit and future schema extension)
        public string RawPayload { get; set; }

        // Flexible metadata column (json) for future expansions
        public string Metadata { get; set; }
    }
}
