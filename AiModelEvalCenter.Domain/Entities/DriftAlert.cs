using System;

namespace AiModelEvalCenter.Domain.Entities
{
    /// <summary>
    /// Bir modelin drift eşiğini aştığı an oluşturulan uyarı kaydı.
    /// </summary>
    public class DriftAlert
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ModelId { get; set; }
        public AiModel? Model { get; set; }

        /// <summary>Uyarıyı tetikleyen ortalama IoU skoru</summary>
        public decimal TriggeredAtIou { get; set; }

        /// <summary>Kaç inference'ın ortalaması alındı</summary>
        public int WindowSize { get; set; }

        /// <summary>Eşik değeri (örn: 0.70)</summary>
        public decimal Threshold { get; set; }

        public string Severity { get; set; } = "WARNING"; // WARNING | CRITICAL

        public DateTimeOffset DetectedAt { get; set; } = DateTimeOffset.UtcNow;

        public bool IsResolved { get; set; } = false;
    }
}
