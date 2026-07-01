using AiModelEvalCenter.Domain.Entities;
using AiModelEvalCenter.Domain.Interfaces;
using AiModelEvalCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AiModelEvalCenter.WebApi.Consumers
{
    public class TelemetryConsumer : BackgroundService
    {
        private readonly ILogger<TelemetryConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConnectionFactory _factory;
        private IConnection? _connection;
        private IChannel? _channel;

        public TelemetryConsumer(ILogger<TelemetryConsumer> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _factory = new ConnectionFactory { HostName = "localhost" };
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _connection = await _factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.ExchangeDeclareAsync("telemetry.exchange", ExchangeType.Topic, cancellationToken: cancellationToken);
            await _channel.QueueDeclareAsync("q.telemetry.ingest", durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
            
            await _channel.QueueBindAsync("q.telemetry.ingest", "telemetry.exchange", "telemetry.#", cancellationToken: cancellationToken);
            await _channel.BasicQosAsync(0, 5, false, cancellationToken); // Prefetch 5

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null) return;

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var messageStr = Encoding.UTF8.GetString(body);
                var routingKey = ea.RoutingKey;

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var driftCalc = scope.ServiceProvider.GetRequiredService<IDriftCalculationService>();

                    bool success = false;

                    if (routingKey == "telemetry.frame")
                    {
                        var frame = JsonSerializer.Deserialize<TelemetryFrame>(messageStr);
                        if (frame != null)
                        {
                            db.TelemetryFrames.Add(frame);
                            await db.SaveChangesAsync();
                            success = true;
                        }
                    }
                    else if (routingKey == "telemetry.groundtruth")
                    {
                        var truth = JsonSerializer.Deserialize<GroundTruth>(messageStr);
                        if (truth != null)
                        {
                            db.GroundTruths.Add(truth);
                            await db.SaveChangesAsync();
                            success = true;
                        }
                    }
                    else if (routingKey == "telemetry.inference")
                    {
                        var inference = JsonSerializer.Deserialize<ModelInference>(messageStr);
                        if (inference != null)
                        {
                            db.ModelInferences.Add(inference);
                            
                            // Calculate Drift
                            var truth = await db.GroundTruths.FirstOrDefaultAsync(g => g.FrameId == inference.FrameId);
                            if (truth != null)
                            {
                                var drift = driftCalc.CalculateDrift(inference, truth);
                                db.DriftMetrics.Add(drift);
                            }
                            
                            await db.SaveChangesAsync();
                            success = true;
                        }
                    }

                    if (success)
                    {
                        // EXPLICIT ACK ONLY AFTER DB SAVE
                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                        _logger.LogInformation("Processed and ACKed {RoutingKey}", routingKey);
                    }
                    else
                    {
                        // Parse hatasi vb
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message {RoutingKey}. NACKing and requeueing.", routingKey);
                    // HATA ALIRSAK MESAJI KUYRUĞA GERİ KOY (RESILIENCE)
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
                }
            };

            await _channel.BasicConsumeAsync("q.telemetry.ingest", autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_channel != null) await _channel.CloseAsync(cancellationToken: cancellationToken);
            if (_connection != null) await _connection.CloseAsync(cancellationToken: cancellationToken);
            await base.StopAsync(cancellationToken);
        }
    }
}
