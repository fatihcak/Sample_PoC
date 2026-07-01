using System;
using System.Text.Json;
using AiModelEvalCenter.Domain.ValueObjects;

namespace AiModelEvalCenter.Domain.Entities
{
    public class ModelInference
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid FrameId { get; set; }
        public TelemetryFrame? Frame { get; set; }
        
        public Guid ModelId { get; set; }
        public AiModel? Model { get; set; }
        
        public required string DetectedClass { get; set; }
        public decimal ConfidenceScore { get; set; } // 0 to 1
        
        public BoundingBox? BoundingBox { get; set; }
        public decimal? InferenceLatencyMs { get; set; }
        public JsonDocument? RawOutput { get; set; }
        
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
