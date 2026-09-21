using System.Threading.Tasks;
using KafkaConsumerMicroService.Data;

namespace KafkaConsumerMicroService.Services
{
    public interface IRecordDispatcher
    {
        Task StoreAndBroadcastAsync(MarketRecord record);
    }
}
