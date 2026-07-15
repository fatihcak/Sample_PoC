using AiModelEvalCenter.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AiModelEvalCenter.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _db;
        public DashboardController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var totalFrames = await _db.TelemetryFrames.CountAsync();
            var totalInferences = await _db.ModelInferences.CountAsync();
            var driftScores = await _db.DriftMetrics.Select(d => d.IouScore).ToListAsync();
            
            var avgDrift = driftScores.Any() ? driftScores.Average(s => (decimal?)s) ?? 0 : 0;
            var activeModels = await _db.AiModels.CountAsync(m => m.IsActive);
            
            return Ok(new 
            { 
                totalFrames, 
                totalInferences, 
                avgDrift = Math.Round(avgDrift, 3),
                activeModels
            });
        }

        [HttpGet("drift-metrics")]
        public async Task<IActionResult> GetDriftMetrics([FromQuery] int limit = 50)
        {
            var metrics = await _db.DriftMetrics
                .Include(d => d.Inference)
                .ThenInclude(i => i.Model)
                .OrderByDescending(d => d.ComputedAt)
                .Take(limit)
                .Select(d => new 
                {
                    id = d.Id,
                    modelName = d.Inference!.Model!.Name,
                    iouScore = d.IouScore,
                    confidenceDelta = d.ConfidenceDelta,
                    classificationCorrect = d.ClassificationCorrect,
                    computedAt = d.ComputedAt,
                    latency = d.Inference.InferenceLatencyMs
                })
                .ToListAsync();

            return Ok(metrics.OrderBy(m => m.computedAt)); // Time series
        }
        [HttpGet("drift-alerts")]
        public async Task<IActionResult> GetDriftAlerts([FromQuery] int limit = 20)
        {
            var alerts = await _db.DriftAlerts
                .Include(a => a.Model)
                .OrderByDescending(a => a.DetectedAt)
                .Take(limit)
                .Select(a => new
                {
                    id = a.Id,
                    modelName = a.Model!.Name,
                    severity = a.Severity,
                    triggeredAtIou = Math.Round(a.TriggeredAtIou, 3),
                    threshold = a.Threshold,
                    windowSize = a.WindowSize,
                    detectedAt = a.DetectedAt,
                    isResolved = a.IsResolved
                })
                .ToListAsync();

            return Ok(alerts);
        }
    }
}
