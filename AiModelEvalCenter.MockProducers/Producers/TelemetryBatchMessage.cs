using AiModelEvalCenter.Domain.Entities;
using AiModelEvalCenter.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace AiModelEvalCenter.MockProducers.Producers
{
    /// <summary>
    /// Tüm bir frame döngüsünü tek mesajda taşıyan Batch wrapper.
    /// Consumer bunu tek transaction içinde atomik olarak işler.
    /// </summary>
    public class TelemetryBatchMessage
    {
        public TelemetryFrame Frame { get; set; } = null!;
        public GroundTruth GroundTruth { get; set; } = null!;
        public List<ModelInference> Inferences { get; set; } = new();
    }
}
