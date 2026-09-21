using KafkaConsumerMicroService.Data;

namespace KafkaConsumerMicroService.Services
{
    public class RecordValidator : IRecordValidator
    {
        // Business rule: PnL amount zero => invalid
        public bool IsValid(MarketRecord record)
        {
            if (record == null) return false;
            return System.Math.Abs(record.PnLAmount) > double.Epsilon;
        }
    }
}
