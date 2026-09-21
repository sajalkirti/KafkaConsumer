using System.Threading.Tasks;

namespace KafkaConsumerMicroService.Services
{
    public interface IValidationService
    {
        Task ProcessMessageAsync(string rawPayload);
    }
}
