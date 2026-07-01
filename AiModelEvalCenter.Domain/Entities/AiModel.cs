using System;

namespace AiModelEvalCenter.Domain.Entities
{
    public class AiModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string Name { get; set; }
        public required string Version { get; set; }
        public required string ModelType { get; set; } // e.g. 'object_detection'
        public string? Framework { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
