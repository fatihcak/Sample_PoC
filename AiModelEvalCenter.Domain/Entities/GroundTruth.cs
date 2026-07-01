using System;
using AiModelEvalCenter.Domain.ValueObjects;

namespace AiModelEvalCenter.Domain.Entities
{
    public class GroundTruth
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid FrameId { get; set; }
        public TelemetryFrame? Frame { get; set; }
        
        public required string VerifiedBy { get; set; }
        public required string VerificationMethod { get; set; }
        public required string TrueClass { get; set; }
        public BoundingBox? BoundingBox { get; set; }
        
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
