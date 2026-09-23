using System;

namespace KafkaConsumerMicroService.Dtos
{
    // Lightweight projection used for fast queries and transport
    public class MarketRecordDto
    {
        public Guid Id { get; set; }
        public string SourceSystem { get; set; }
        public long AccountNumber { get; set; }
        public double PnLAmount { get; set; }
        public bool IsValid { get; set; }
        public DateTimeOffset ReceivedAt { get; set; }
        // include metadata but exclude raw payload by default for performance
        public string Metadata { get; set; }
    }
}
