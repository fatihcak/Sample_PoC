using AiModelEvalCenter.Domain.Entities;
using AiModelEvalCenter.Domain.Messages;
using AiModelEvalCenter.Domain.ValueObjects;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AiModelEvalCenter.MockProducers.Producers
{
    public class MockTelemetryProducer : BackgroundService
    {
        private readonly ILogger<MockTelemetryProducer> _logger;
        private readonly ConnectionFactory _factory;
        private readonly Guid _sessionId = Guid.NewGuid();
        private long _frameSequence = 0;

        // Model Seeds we defined in Phase 1
        private readonly Guid[] _modelIds = new[]
        {
            Guid.Parse("11111111-1111-1111-1111-111111111111"), // YOLOv8
            Guid.Parse("22222222-2222-2222-2222-222222222222"), // RT-DETR
            Guid.Parse("33333333-3333-3333-3333-333333333333")  // Thermal
        };

        public MockTelemetryProducer(ILogger<MockTelemetryProducer> logger)
        {
            _logger = logger;
            _factory = new ConnectionFactory { HostName = "localhost" };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Mock Producer is starting.");

            await using var connection = await _factory.CreateConnectionAsync(stoppingToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await channel.ExchangeDeclareAsync("telemetry.exchange", ExchangeType.Topic, cancellationToken: stoppingToken);

            // Publish session FIRST so Consumer can satisfy FK constraint before any frame arrives
            var session = new AiModelEvalCenter.Domain.Entities.TelemetrySession
            {
                Id = _sessionId,
                AircraftId = "BAYRAKTAR-TB2-SIM-001",
                SessionLabel = $"MockSession-{DateTime.UtcNow:yyyyMMdd-HHmmss}",
                StartedAt = DateTimeOffset.UtcNow,
                Status = AiModelEvalCenter.Domain.Enums.SessionStatus.Running
            };
            await PublishMessage(channel, "telemetry.session", session, stoppingToken);
            _logger.LogInformation("Published Session {SessionId}", _sessionId);

            // Small delay to give Consumer time to commit the session before first frame
            await Task.Delay(500, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                _frameSequence++;
                var frameId = Guid.NewGuid();
                
                var truthBox = new BoundingBox { X = 100, Y = 100, W = 50, H = 50 };

                var batch = new TelemetryBatchMessage
                {
                    Frame = new TelemetryFrame
                    {
                        Id = frameId,
                        SessionId = _sessionId,
                        FrameSequence = _frameSequence,
                        CapturedAt = DateTimeOffset.UtcNow,
                        AltitudeM = 1500m + (decimal)(new Random().NextDouble() * 100 - 50),
                        VelocityMps = 200m + (decimal)(new Random().NextDouble() * 20 - 10),
                        HeadingDeg = 45m
                    },
                    GroundTruth = new GroundTruth
                    {
                        Id = Guid.NewGuid(),
                        FrameId = frameId,
                        VerifiedBy = "SimOracle",
                        VerificationMethod = "simulation",
                        TrueClass = "drone",
                        BoundingBox = truthBox
                    },
                    Inferences = new System.Collections.Generic.List<ModelInference>()
                };

                foreach (var modelId in _modelIds)
                {
                    batch.Inferences.Add(new ModelInference
                    {
                        Id = Guid.NewGuid(),
                        FrameId = frameId,
                        ModelId = modelId,
                        DetectedClass = "drone",
                        ConfidenceScore = 0.8m + (decimal)(new Random().NextDouble() * 0.15),
                        BoundingBox = new BoundingBox 
                        { 
                            X = truthBox.X + (new Random().NextDouble() * 10 - 5),
                            Y = truthBox.Y + (new Random().NextDouble() * 10 - 5),
                            W = truthBox.W, 
                            H = truthBox.H 
                        },
                        InferenceLatencyMs = 40m + (decimal)(new Random().NextDouble() * 20)
                    });
                }

                // Single atomic message — no more race conditions!
                await PublishMessage(channel, "telemetry.batch", batch, stoppingToken);

                _logger.LogInformation("Published Batch Frame {Sequence}", _frameSequence);
                
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task PublishMessage<T>(IChannel channel, string routingKey, T message, CancellationToken stoppingToken)
        {
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            await channel.BasicPublishAsync(
                exchange: "telemetry.exchange",
                routingKey: routingKey,
                body: body,
                cancellationToken: stoppingToken);
        }
    }
}
