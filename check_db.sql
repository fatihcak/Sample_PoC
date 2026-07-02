SELECT 
  (SELECT count(*) FROM "TelemetryFrames") as frames,
  (SELECT count(*) FROM "ModelInferences") as inferences,
  (SELECT count(*) FROM "DriftMetrics") as drift_metrics,
  (SELECT count(*) FROM "GroundTruths") as ground_truths;
