using AiModelEvalCenter.Domain.Entities;
using AiModelEvalCenter.Domain.Interfaces;
using AiModelEvalCenter.Domain.Messages;
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
            await _channel.BasicQosAsync(0, 1, false, cancellationToken); // Prefetch 1 — sequential processing

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

                    if (routingKey == "telemetry.session")
                    {
                        var session = JsonSerializer.Deserialize<TelemetrySession>(messageStr);
                        if (session != null)
                        {
                            var existing = await db.TelemetrySessions.FindAsync(session.Id);
                            if (existing == null)
                            {
                                db.TelemetrySessions.Add(session);
                                await db.SaveChangesAsync();
                            }
                            success = true;
                        }
                    }
                    else if (routingKey == "telemetry.batch")
                    {
                        var batch = JsonSerializer.Deserialize<TelemetryBatchMessage>(messageStr);
                        if (batch != null)
                        {
                            // Atomik işlem: Frame → GroundTruth → Inferences → DriftMetrics
                            db.TelemetryFrames.Add(batch.Frame);
                            await db.SaveChangesAsync(); // Frame önce yazılmak zorunda (FK)

                            db.GroundTruths.Add(batch.GroundTruth);
                            await db.SaveChangesAsync(); // GroundTruth ikinci (Frame FK'ya bağlı)

                            foreach (var inference in batch.Inferences)
                            {
                                db.ModelInferences.Add(inference);
                                await db.SaveChangesAsync(); // Her inference tek tek (DriftCalc için)

                                // Drift hesapla
                                var drift = driftCalc.CalculateDrift(inference, batch.GroundTruth);
                                db.DriftMetrics.Add(drift);
                                await db.SaveChangesAsync();
                            }

                            success = true;
                        }
                    }

                    if (success)
                    {
                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                        _logger.LogInformation("ACKed {RoutingKey}", routingKey);
                    }
                    else
                    {
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, false); // Dead-letter
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing {RoutingKey} — NACKing with requeue.", routingKey);
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
