using System;
using AiModelEvalCenter.Domain.Enums;

namespace AiModelEvalCenter.Domain.Entities
{
    public class TelemetrySession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string AircraftId { get; set; }
        public string? SessionLabel { get; set; }
        public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? EndedAt { get; set; }
        public SessionStatus Status { get; set; } = SessionStatus.Running;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
