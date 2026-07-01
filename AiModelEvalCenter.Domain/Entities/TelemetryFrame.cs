using System;

namespace AiModelEvalCenter.Domain.Entities
{
    public class TelemetryFrame
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid SessionId { get; set; }
        public TelemetrySession? Session { get; set; }
        
        public long FrameSequence { get; set; }
        public DateTimeOffset CapturedAt { get; set; }
        
        public decimal? AltitudeM { get; set; }
        public decimal? VelocityMps { get; set; }
        public decimal? HeadingDeg { get; set; }
        public string? SensorPayloadUri { get; set; }
        
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
