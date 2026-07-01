using AiModelEvalCenter.Domain.Entities;
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

            while (!stoppingToken.IsCancellationRequested)
            {
                _frameSequence++;
                var frameId = Guid.NewGuid();
                
                // 1. Generate Telemetry
                var telemetry = new TelemetryFrame
                {
                    Id = frameId,
                    SessionId = _sessionId,
                    FrameSequence = _frameSequence,
                    CapturedAt = DateTimeOffset.UtcNow,
                    AltitudeM = 1500m + (decimal)(new Random().NextDouble() * 100 - 50),
                    VelocityMps = 200m + (decimal)(new Random().NextDouble() * 20 - 10),
                    HeadingDeg = 45m
                };

                // 2. Generate Ground Truth (Kritik ekleme - MVP)
                var truthBox = new BoundingBox { X = 100, Y = 100, W = 50, H = 50 };
                var groundTruth = new GroundTruth
                {
                    Id = Guid.NewGuid(),
                    FrameId = frameId,
                    VerifiedBy = "SimOracle",
                    VerificationMethod = "simulation",
                    TrueClass = "drone",
                    BoundingBox = truthBox
                };

                // Publish Telemetry
                await PublishMessage(channel, "telemetry.frame", telemetry, stoppingToken);
                await PublishMessage(channel, "telemetry.groundtruth", groundTruth, stoppingToken);

                // 3. Generate Inferences (biraz sapmalı/hatalı)
                foreach (var modelId in _modelIds)
                {
                    var inference = new ModelInference
                    {
                        Id = Guid.NewGuid(),
                        FrameId = frameId,
                        ModelId = modelId,
                        DetectedClass = "drone", // %10 ihtimalle yanlış tahmin yapabiliriz ama basitleştirelim
                        ConfidenceScore = 0.8m + (decimal)(new Random().NextDouble() * 0.15),
                        BoundingBox = new BoundingBox 
                        { 
                            X = truthBox.X + (new Random().NextDouble() * 10 - 5), // Sapma
                            Y = truthBox.Y + (new Random().NextDouble() * 10 - 5),
                            W = truthBox.W, 
                            H = truthBox.H 
                        },
                        InferenceLatencyMs = 40m + (decimal)(new Random().NextDouble() * 20)
                    };

                    await PublishMessage(channel, "telemetry.inference", inference, stoppingToken);
                }

                _logger.LogInformation("Published Frame {Sequence}", _frameSequence);
                
                await Task.Delay(1000, stoppingToken); // 1 mesaj/sn
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
