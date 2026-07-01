using AiModelEvalCenter.Domain.Entities;
using AiModelEvalCenter.Domain.Interfaces;
using AiModelEvalCenter.Domain.ValueObjects;
using System;

namespace AiModelEvalCenter.Infrastructure.Services
{
    public class DriftCalculationService : IDriftCalculationService
    {
        public DriftMetric CalculateDrift(ModelInference inference, GroundTruth truth)
        {
            var iou = CalculateIoU(inference.BoundingBox, truth.BoundingBox);
            var confidenceDelta = Math.Abs(1.0m - inference.ConfidenceScore);
            var isCorrect = string.Equals(inference.DetectedClass, truth.TrueClass, StringComparison.OrdinalIgnoreCase);

            return new DriftMetric
            {
                InferenceId = inference.Id,
                GroundTruthId = truth.Id,
                IouScore = iou,
                ConfidenceDelta = confidenceDelta,
                ClassificationCorrect = isCorrect,
                ComputedAt = DateTimeOffset.UtcNow
            };
        }

        private decimal? CalculateIoU(BoundingBox? boxA, BoundingBox? boxB)
        {
            if (boxA == null || boxB == null) return null;

            // IoU Calculation Logic
            var xA = Math.Max(boxA.X, boxB.X);
            var yA = Math.Max(boxA.Y, boxB.Y);
            var xB = Math.Min(boxA.X + boxA.W, boxB.X + boxB.W);
            var yB = Math.Min(boxA.Y + boxA.H, boxB.Y + boxB.H);

            var intersectionArea = Math.Max(0, xB - xA) * Math.Max(0, yB - yA);

            var boxAArea = boxA.W * boxA.H;
            var boxBArea = boxB.W * boxB.H;

            var unionArea = boxAArea + boxBArea - intersectionArea;

            if (unionArea == 0) return 0;

            return (decimal)(intersectionArea / unionArea);
        }
    }
}
