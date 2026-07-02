using AiModelEvalCenter.Domain.Entities;
using System.Collections.Generic;

namespace AiModelEvalCenter.Domain.Messages
{
    /// <summary>
    /// Tüm bir telemetri döngüsünü (Frame + GroundTruth + Inferences) 
    /// tek bir atomik RabbitMQ mesajında taşıyan wrapper.
    /// Race condition'ı önler: Consumer bu mesajı sıralı ve atomik olarak işler.
    /// </summary>
    public class TelemetryBatchMessage
    {
        public TelemetryFrame Frame { get; set; } = null!;
        public GroundTruth GroundTruth { get; set; } = null!;
        public List<ModelInference> Inferences { get; set; } = new();
    }
}
