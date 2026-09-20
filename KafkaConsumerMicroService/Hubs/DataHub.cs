using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace KafkaConsumerMicroService.Hubs
{
    // Simple SignalR hub used to broadcast validated/invalidated PnL records to connected UIs
    public class DataHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        // Client can call this to subscribe or test connectivity
        public Task Subscribe() => Task.CompletedTask;
    }
}
