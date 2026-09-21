using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace KafkaConsumerMicroService.Services
{
    // Background service responsible for consuming messages from Kafka (or a simulated source)
    public class KafkaConsumerService : BackgroundService
    {
        private readonly ILogger<KafkaConsumerService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;

        public KafkaConsumerService(ILogger<KafkaConsumerService> logger, IServiceScopeFactory scopeFactory, IConfiguration config)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("KafkaConsumerService starting.");

            // In this implementation we provide a simulated message generator to demonstrate end-to-end flow.
            // For production, implement the real Kafka consumer using Confluent.Kafka and configuration values.

            var mode = _config.GetValue<string>("Kafka:Mode") ?? "Simulated";

            if (mode.Equals("Real", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Real Kafka mode requested but not implemented in this template. Configure Confluent.Kafka consumer here.");
                // TODO: Wire up Confluent.Kafka consumer, subscribe to topics and call _validator.ProcessMessageAsync for each message.
            }
            else
            {
                await RunSimulatedProducer(stoppingToken);
            }
        }

        private async Task RunSimulatedProducer(CancellationToken stoppingToken)
        {
            // Create deterministic simulated messages to aid testing and UI demo
            var rnd = new Random();
            var sources = new[] { "XFeed", "RealtimeBus", "ExtSystem" };

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var msg = new
                    {
                        SourceSystem = sources[rnd.Next(sources.Length)],
                        AccountNumber = rnd.Next(1000, 9999),
                        PnLAmount = Math.Round((rnd.NextDouble() - 0.5) * 2000.0, 2) // can be negative/positive
                    };

                    // Occasionally emit zero to simulate invalid record
                    if (rnd.NextDouble() < 0.05)
                    {
                        msg = new { msg.SourceSystem, msg.AccountNumber, PnLAmount = 0.0 };
                    }

                    var payload = JsonSerializer.Serialize(msg);

                    // Resolve the scoped validation service from a new scope for each message (best practice)
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var validator = scope.ServiceProvider.GetRequiredService<IValidationService>();
                        await validator.ProcessMessageAsync(payload);
                    }

                    // Throttle: in production tune concurrency/parallelism and use partitioned consumers
                    await Task.Delay(250, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in simulated producer loop");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}
