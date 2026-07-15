using AiModelEvalCenter.Domain.Entities;
using AiModelEvalCenter.Domain.Interfaces;
using AiModelEvalCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AiModelEvalCenter.Infrastructure.Services
{
    public class DriftAlertService : IDriftAlertService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<DriftAlertService> _logger;

        // Eşik değerleri
        private const decimal WarningThreshold = 0.70m;  // IoU < 0.70 → WARNING
        private const decimal CriticalThreshold = 0.50m; // IoU < 0.50 → CRITICAL

        public DriftAlertService(AppDbContext db, ILogger<DriftAlertService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<DriftAlert?> CheckForDriftAsync(Guid modelId, int windowSize = 10)
        {
            // Son N inference'ın IoU ortalamasını hesapla
            var recentMetrics = await _db.DriftMetrics
                .Where(d => d.Inference != null && d.Inference.ModelId == modelId)
                .OrderByDescending(d => d.ComputedAt)
                .Take(windowSize)
                .Select(d => d.IouScore)
                .ToListAsync();

            // Yeterli veri yoksa kontrol etme
            if (recentMetrics.Count < windowSize)
                return null;

            var avgIou = recentMetrics.Average();

            // Eşik altındaysa alert üret
            if (avgIou < CriticalThreshold)
            {
                _logger.LogError("🚨 CRITICAL DRIFT — Model {ModelId} Avg IoU: {Iou:F3}", modelId, avgIou);
                return new DriftAlert
                {
                    ModelId = modelId,
                    TriggeredAtIou = (decimal)avgIou,
                    WindowSize = windowSize,
                    Threshold = CriticalThreshold,
                    Severity = "CRITICAL"
                };
            }

            if (avgIou < WarningThreshold)
            {
                _logger.LogWarning("⚠️ WARNING DRIFT — Model {ModelId} Avg IoU: {Iou:F3}", modelId, avgIou);
                return new DriftAlert
                {
                    ModelId = modelId,
                    TriggeredAtIou = (decimal)avgIou,
                    WindowSize = windowSize,
                    Threshold = WarningThreshold,
                    Severity = "WARNING"
                };
            }

            return null; // Her şey yolunda
        }
    }
}
