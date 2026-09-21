using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
                _logger.LogInformation("Starting real Kafka consumer mode.");
                await RunRealConsumer(stoppingToken);
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

        private async Task RunRealConsumer(CancellationToken stoppingToken)
        {
            // Attempt to use Confluent.Kafka for real consumption. If the assembly is not present
            // this will surface at runtime; catch and log to allow running in simulated mode.
            try
            {
                var kafkaSection = _config.GetSection("Kafka");
                var bootstrap = kafkaSection.GetValue<string>("BootstrapServers") ?? "localhost:9092";
                var topic = kafkaSection.GetValue<string>("Topic") ?? "pnl-updates";
                var groupId = kafkaSection.GetValue<string>("GroupId") ?? "pnl-consumer-group";

                var config = new Confluent.Kafka.ConsumerConfig
                {
                    BootstrapServers = bootstrap,
                    GroupId = groupId,
                    AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest,
                    EnableAutoCommit = false
                };

                using var consumer = new Confluent.Kafka.ConsumerBuilder<Confluent.Kafka.Ignore, string>(config)
                    .SetErrorHandler((_, e) => _logger.LogError("Kafka error: {reason}", e.Reason))
                    .Build();

                consumer.Subscribe(topic);
                _logger.LogInformation("Subscribed to topic {topic}", topic);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(stoppingToken);
                        if (result?.Message?.Value != null)
                        {
                            var payload = result.Message.Value;
                            using (var scope = _scopeFactory.CreateScope())
                            {
                                var validator = scope.ServiceProvider.GetRequiredService<IValidationService>();
                                await validator.ProcessMessageAsync(payload);
                            }

                            // commit offset
                            try { consumer.Commit(result); } catch (Exception ex) { _logger.LogWarning(ex, "Commit failed"); }
                        }
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Confluent.Kafka.KafkaException kex)
                    {
                        _logger.LogError(kex, "Kafka exception in consume loop");
                        await Task.Delay(1000, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error in Kafka consume loop");
                        await Task.Delay(1000, stoppingToken);
                    }
                }

                try { consumer.Close(); } catch { }
            }
            catch (System.IO.FileNotFoundException)
            {
                _logger.LogWarning("Confluent.Kafka not found. Install the Confluent.Kafka NuGet package and set Kafka:Mode=Real to enable real consumption.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start real Kafka consumer");
            }
        }
    }
}
