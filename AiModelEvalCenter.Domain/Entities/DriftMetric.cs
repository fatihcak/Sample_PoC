using System;

namespace AiModelEvalCenter.Domain.Entities
{
    public class DriftMetric
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid InferenceId { get; set; }
        public ModelInference? Inference { get; set; }
        
        public Guid GroundTruthId { get; set; }
        public GroundTruth? GroundTruth { get; set; }
        
        public decimal? IouScore { get; set; } // Intersection over Union
        public decimal? ConfidenceDelta { get; set; } // |predicted_confidence - 1.0|
        public bool ClassificationCorrect { get; set; }
        
        public DateTimeOffset ComputedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
