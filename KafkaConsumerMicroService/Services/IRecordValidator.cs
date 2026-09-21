using KafkaConsumerMicroService.Data;

namespace KafkaConsumerMicroService.Services
{
    public interface IRecordValidator
    {
        bool IsValid(MarketRecord record);
    }
}
