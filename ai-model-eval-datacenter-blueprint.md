# AI Model Evaluation & Comparison Data Center — System Architecture Blueprint

**Document Type:** Proof of Concept (PoC) Architecture Specification
**Domain:** Aviation / Defense — AI Model Telemetry & Drift Evaluation
**Prepared as:** Clean Architecture Reference Design (.NET Core 8 / PostgreSQL / RabbitMQ / React+TS)

---

## 1. Database Schema (PostgreSQL)

### 1.1 Design Rationale

The schema separates four concerns that must evolve independently:

- **Model Registry** — what models exist, their versions, and metadata (so you can compare v1 vs v2 of the same detector).
- **Telemetry** — raw operational signals from the aircraft/sensor context at the moment of inference (altitude, velocity, sensor health, timestamp).
- **Inference Results** — what each model *predicted* (bounding boxes, class, confidence) for a given telemetry frame.
- **Ground Truth** — the verified correct answer for a given frame, used to compute drift/accuracy against inferences.

This 4-way separation is what lets you compute **per-model drift over time** without polluting the raw sensor stream with model opinions.

### 1.2 Entity-Relationship Overview (Text Diagram)

```
 ┌─────────────────┐        ┌──────────────────────┐
 │   ai_models      │        │   telemetry_sessions │
 │──────────────────│        │───────────────────────│
 │ id (PK)          │        │ id (PK)               │
 │ name             │        │ aircraft_id            │
 │ version          │        │ started_at              │
 │ model_type       │        │ ended_at                │
 │ created_at       │        │ status                  │
 └────────┬─────────┘        └───────────┬─────────────┘
          │                              │
          │ 1..N                         │ 1..N
          ▼                              ▼
 ┌──────────────────────────────────────────────┐
 │              telemetry_frames                  │
 │────────────────────────────────────────────────│
 │ id (PK)                                         │
 │ session_id (FK -> telemetry_sessions.id)        │
 │ frame_sequence                                  │
 │ captured_at                                     │
 │ altitude_m, velocity_mps, heading_deg           │
 │ sensor_payload_uri  (raw image/lidar ref)       │
 │ created_at                                      │
 └───────────┬───────────────────────┬─────────────┘
             │ 1..N                  │ 1..1
             ▼                       ▼
 ┌───────────────────────┐  ┌────────────────────────┐
 │  model_inferences      │  │   ground_truths          │
 │────────────────────────│  │──────────────────────────│
 │ id (PK)                 │  │ id (PK)                   │
 │ frame_id (FK)            │  │ frame_id (FK, UNIQUE)      │
 │ model_id (FK)            │  │ verified_by                │
 │ detected_class            │  │ verification_method        │
 │ confidence_score          │  │ bounding_box (jsonb)        │
 │ bounding_box (jsonb)      │  │ created_at                   │
 │ inference_latency_ms      │  └──────────────────────────────┘
 │ raw_output (jsonb)        │
 │ created_at                │
 └───────────┬────────────────┘
             │ 1..1 (derived, computed on write or via view)
             ▼
 ┌─────────────────────────────┐
 │   drift_metrics                │
 │─────────────────────────────────│
 │ id (PK)                          │
 │ inference_id (FK, UNIQUE)          │
 │ ground_truth_id (FK)                │
 │ iou_score            (spatial drift) │
 │ confidence_delta                       │
 │ classification_correct (bool)           │
 │ computed_at                              │
 └───────────────────────────────────────────┘
```

### 1.3 DDL

```sql
-- =========================================================
-- 1. MODEL REGISTRY
-- =========================================================
CREATE TABLE ai_models (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(150)    NOT NULL,
    version         VARCHAR(50)     NOT NULL,
    model_type      VARCHAR(100)    NOT NULL,  -- e.g. 'object_detection', 'segmentation'
    framework       VARCHAR(100),              -- e.g. 'ONNX', 'TensorRT'
    description     TEXT,
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT now(),
    UNIQUE (name, version)
);

-- =========================================================
-- 2. TELEMETRY SESSIONS (a simulated "flight run")
-- =========================================================
CREATE TABLE telemetry_sessions (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    aircraft_id     VARCHAR(100)    NOT NULL,
    session_label   VARCHAR(200),
    started_at      TIMESTAMPTZ     NOT NULL DEFAULT now(),
    ended_at        TIMESTAMPTZ,
    status          VARCHAR(30)     NOT NULL DEFAULT 'RUNNING', -- RUNNING | COMPLETED | ABORTED
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT now()
);

-- =========================================================
-- 3. TELEMETRY FRAMES (raw sensor snapshot, model-agnostic)
-- =========================================================
CREATE TABLE telemetry_frames (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    session_id          UUID NOT NULL REFERENCES telemetry_sessions(id) ON DELETE CASCADE,
    frame_sequence      BIGINT NOT NULL,
    captured_at         TIMESTAMPTZ NOT NULL,
    altitude_m          NUMERIC(10,2),
    velocity_mps        NUMERIC(10,2),
    heading_deg         NUMERIC(6,2),
    sensor_payload_uri  TEXT,             -- pointer to blob storage (image/lidar frame)
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (session_id, frame_sequence)
);
CREATE INDEX idx_telemetry_frames_session_time ON telemetry_frames (session_id, captured_at);

-- =========================================================
-- 4. MODEL INFERENCES (what each model predicted per frame)
-- =========================================================
CREATE TABLE model_inferences (
    id                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    frame_id             UUID NOT NULL REFERENCES telemetry_frames(id) ON DELETE CASCADE,
    model_id             UUID NOT NULL REFERENCES ai_models(id) ON DELETE RESTRICT,
    detected_class        VARCHAR(150) NOT NULL,
    confidence_score       NUMERIC(5,4) NOT NULL CHECK (confidence_score BETWEEN 0 AND 1),
    bounding_box            JSONB,           -- { "x":.., "y":.., "w":.., "h":.. }
    inference_latency_ms    NUMERIC(10,3),
    raw_output               JSONB,          -- full model payload for audit/debug
    created_at                TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_inferences_frame ON model_inferences (frame_id);
CREATE INDEX idx_inferences_model_time ON model_inferences (model_id, created_at);

-- =========================================================
-- 5. GROUND TRUTH (verified correct answer per frame)
-- =========================================================
CREATE TABLE ground_truths (
    id                     UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    frame_id               UUID NOT NULL UNIQUE REFERENCES telemetry_frames(id) ON DELETE CASCADE,
    verified_by            VARCHAR(150) NOT NULL,   -- annotator / oracle system
    verification_method    VARCHAR(100) NOT NULL,   -- 'manual', 'reference_sensor', 'simulation_oracle'
    true_class             VARCHAR(150) NOT NULL,
    bounding_box           JSONB,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- =========================================================
-- 6. DRIFT METRICS (computed comparison: inference vs ground truth)
-- =========================================================
CREATE TABLE drift_metrics (
    id                        UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    inference_id              UUID NOT NULL UNIQUE REFERENCES model_inferences(id) ON DELETE CASCADE,
    ground_truth_id           UUID NOT NULL REFERENCES ground_truths(id) ON DELETE CASCADE,
    iou_score                 NUMERIC(5,4),      -- spatial overlap accuracy
    confidence_delta          NUMERIC(5,4),      -- |predicted_confidence - 1.0| proxy for overconfidence
    classification_correct    BOOLEAN NOT NULL,
    computed_at                TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_drift_ground_truth ON drift_metrics (ground_truth_id);

-- =========================================================
-- 7. AGGREGATE VIEW — for dashboard performance queries
-- =========================================================
CREATE MATERIALIZED VIEW model_performance_summary AS
SELECT
    m.id                         AS model_id,
    m.name                       AS model_name,
    m.version                    AS model_version,
    COUNT(dm.id)                 AS total_evaluations,
    AVG(dm.iou_score)            AS avg_iou,
    AVG(CASE WHEN dm.classification_correct THEN 1 ELSE 0 END) AS accuracy_rate,
    AVG(mi.confidence_score)     AS avg_confidence,
    AVG(mi.inference_latency_ms) AS avg_latency_ms
FROM ai_models m
JOIN model_inferences mi ON mi.model_id = m.id
JOIN drift_metrics dm ON dm.inference_id = mi.id
GROUP BY m.id, m.name, m.version;
```

**Notes on design decisions to be ready to defend in interview:**

- `UUID` primary keys → safe for distributed/simulated multi-node ingestion (no sequence contention across mock model producers).
- `JSONB` for bounding boxes/raw output → schema flexibility across model types (detection vs segmentation) without migrations.
- `drift_metrics` is **decoupled** from `model_inferences` (1:1 via FK) rather than embedded — this lets you recompute drift logic independently (e.g., swap IOU threshold) without touching raw inference records, which is an auditability requirement in regulated/defense contexts.
- `model_performance_summary` is a materialized view, refreshed on a schedule or trigger — dashboard reads should never hit raw joins at scale.

---

## 2. System Architecture Flow

### 2.1 High-Level Data Flow

```
┌────────────────────┐
│  Mock AI Model      │  (3x simulated producers, one per model)
│  Producers (Console  │  - Generate synthetic telemetry_frame + inference payloads
│  Workers / BackgroundServices)│  - Publish at configurable interval (e.g. every 200ms)
└──────────┬──────────┘
           │  publish (AMQP)
           ▼
┌────────────────────┐
│     RabbitMQ         │
│  Exchange: telemetry.exchange (topic)
│  Routing keys:
│    - telemetry.frame.created
│    - telemetry.inference.completed
│  Queues:
│    - q.telemetry.ingest
│    - q.inference.ingest
└──────────┬──────────┘
           │  consume
           ▼
┌─────────────────────────────┐
│  .NET Core 8 Consumer Service │  (BackgroundService / Hosted Service)
│  - Deserializes AMQP message   │
│  - Validates payload             │
│  - Persists via EF Core / Dapper  │
│  - Triggers drift computation      │
│    (compares against ground_truths) │
│  - Publishes 'drift.computed' event   │
│    back to RabbitMQ (optional, for      │
│    real-time push to frontend)            │
└──────────┬───────────────────────────────┘
           │  write
           ▼
┌────────────────────┐
│   PostgreSQL          │
│  - telemetry_frames     │
│  - model_inferences       │
│  - ground_truths             │
│  - drift_metrics                │
│  - model_performance_summary (MV) │
└──────────┬──────────┘
           │  read (REST) + push (SignalR/WebSocket)
           ▼
┌─────────────────────────────┐
│  ASP.NET Core Web API           │
│  - REST endpoints for historical  │
│    queries & dashboard bootstrap    │
│  - SignalR Hub for real-time drift    │
│    push (avoids polling)                │
└──────────┬───────────────────────────────┘
           │  HTTPS / WebSocket
           ▼
┌────────────────────┐
│  React + TypeScript   │
│  Dashboard              │
│  - Live drift charts       │
│  - Model comparison table     │
│  - Session replay view           │
└────────────────────┘
```

### 2.2 Why This Flow (architectural justification)

- **RabbitMQ as the decoupling layer** between mock producers and persistence means you can kill/restart the consumer without losing in-flight telemetry (durable queues + manual ack), and you can scale consumers horizontally per queue if throughput increases — directly relevant to a defense-context requirement of **no silent data loss**.
- **Separate routing keys for frame vs inference** allow independent consumer scaling — telemetry frames may arrive at a different cadence than inference results (inference has computation latency).
- **SignalR (not polling)** for the dashboard's real-time layer — polling a REST endpoint every second doesn't scale and adds artificial latency to a "real-time" narrative; a defense-reviewer will notice if you reach for polling here.
- **Drift computation on ingest, not on read** — precomputing `drift_metrics` at write-time keeps dashboard reads fast (indexed lookups) rather than requiring on-the-fly IOU calculation per dashboard refresh.

---

## 3. Folder Structure

### 3.1 .NET Core Backend (Clean Architecture)

```
AiModelEvalCenter.Backend/
│
├── src/
│   ├── AiModelEvalCenter.Domain/                  # Enterprise business rules — no dependencies
│   │   ├── Entities/
│   │   │   ├── AiModel.cs
│   │   │   ├── TelemetrySession.cs
│   │   │   ├── TelemetryFrame.cs
│   │   │   ├── ModelInference.cs
│   │   │   ├── GroundTruth.cs
│   │   │   └── DriftMetric.cs
│   │   ├── Enums/
│   │   │   └── SessionStatus.cs
│   │   ├── ValueObjects/
│   │   │   └── BoundingBox.cs
│   │   └── Interfaces/
│   │       ├── IAiModelRepository.cs
│   │       ├── ITelemetryRepository.cs
│   │       └── IDriftCalculationService.cs
│   │
│   ├── AiModelEvalCenter.Application/             # Use cases / orchestration
│   │   ├── UseCases/
│   │   │   ├── Telemetry/
│   │   │   │   ├── IngestTelemetryFrameCommand.cs
│   │   │   │   └── IngestTelemetryFrameHandler.cs
│   │   │   ├── Inference/
│   │   │   │   ├── RecordInferenceCommand.cs
│   │   │   │   └── RecordInferenceHandler.cs
│   │   │   └── Drift/
│   │   │       ├── ComputeDriftCommand.cs
│   │   │       └── ComputeDriftHandler.cs
│   │   ├── DTOs/
│   │   │   ├── TelemetryFrameDto.cs
│   │   │   ├── InferenceResultDto.cs
│   │   │   └── ModelPerformanceDto.cs
│   │   ├── Interfaces/
│   │   │   └── IMessageBusPublisher.cs
│   │   └── Mappings/
│   │       └── DomainToDtoProfile.cs (AutoMapper)
│   │
│   ├── AiModelEvalCenter.Infrastructure/          # External concerns
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/                    # EF Core Fluent API per entity
│   │   │   │   ├── AiModelConfiguration.cs
│   │   │   │   └── TelemetryFrameConfiguration.cs
│   │   │   ├── Repositories/
│   │   │   │   ├── AiModelRepository.cs
│   │   │   │   └── TelemetryRepository.cs
│   │   │   └── Migrations/
│   │   ├── Messaging/
│   │   │   ├── RabbitMqConnectionFactory.cs
│   │   │   ├── RabbitMqPublisher.cs
│   │   │   └── Consumers/
│   │   │       ├── TelemetryFrameConsumer.cs      # BackgroundService
│   │   │       └── InferenceResultConsumer.cs     # BackgroundService
│   │   └── Services/
│   │       └── DriftCalculationService.cs         # IOU / confidence delta math
│   │
│   ├── AiModelEvalCenter.MockProducers/           # Simulated AI model workers
│   │   ├── Producers/
│   │   │   ├── ObjectDetectionModelSimulator.cs
│   │   │   ├── SegmentationModelSimulator.cs
│   │   │   └── ThermalDetectionModelSimulator.cs
│   │   └── Program.cs                             # Console host, runs 3 simulators
│   │
│   └── AiModelEvalCenter.WebApi/                  # Presentation layer
│       ├── Controllers/
│       │   ├── ModelsController.cs
│       │   ├── TelemetryController.cs
│       │   ├── InferencesController.cs
│       │   └── DriftMetricsController.cs
│       ├── Hubs/
│       │   └── DriftDashboardHub.cs               # SignalR
│       ├── Middleware/
│       │   └── ExceptionHandlingMiddleware.cs
│       ├── Program.cs
│       └── appsettings.json
│
├── tests/
│   ├── AiModelEvalCenter.Domain.Tests/
│   ├── AiModelEvalCenter.Application.Tests/
│   └── AiModelEvalCenter.Infrastructure.Tests/
│
├── docker-compose.yml                             # postgres + rabbitmq + api
└── AiModelEvalCenter.sln
```

### 3.2 React + TypeScript Frontend

```
ai-model-eval-dashboard/
│
├── src/
│   ├── api/
│   │   ├── client.ts                  # axios/fetch base instance
│   │   ├── modelsApi.ts
│   │   ├── telemetryApi.ts
│   │   └── driftApi.ts
│   │
│   ├── hooks/
│   │   ├── useSignalRConnection.ts    # real-time hub subscription
│   │   ├── useModelPerformance.ts
│   │   └── useDriftStream.ts
│   │
│   ├── components/
│   │   ├── common/
│   │   │   ├── Card.tsx
│   │   │   ├── Spinner.tsx
│   │   │   └── ErrorBoundary.tsx
│   │   ├── dashboard/
│   │   │   ├── ModelComparisonTable.tsx
│   │   │   ├── DriftTimelineChart.tsx
│   │   │   ├── ConfidenceHeatmap.tsx
│   │   │   └── SessionSelector.tsx
│   │   └── layout/
│   │       ├── AppShell.tsx
│   │       └── Sidebar.tsx
│   │
│   ├── pages/
│   │   ├── DashboardPage.tsx
│   │   ├── ModelDetailPage.tsx
│   │   └── SessionReplayPage.tsx
│   │
│   ├── types/
│   │   ├── model.types.ts
│   │   ├── telemetry.types.ts
│   │   └── drift.types.ts
│   │
│   ├── store/                         # Redux Toolkit or Zustand
│   │   ├── modelsSlice.ts
│   │   └── driftSlice.ts
│   │
│   ├── utils/
│   │   └── formatters.ts
│   │
│   ├── App.tsx
│   └── main.tsx
│
├── public/
├── .env.example
├── tsconfig.json
├── vite.config.ts
└── package.json
```

---

## 4. API Contracts

### 4.1 `GET /api/v1/models`
List all registered models.

**Response 200**
```json
[
  {
    "id": "b1e2c3d4-...",
    "name": "YOLOv8-Aerial",
    "version": "2.3.1",
    "modelType": "object_detection",
    "isActive": true
  }
]
```

### 4.2 `GET /api/v1/models/{modelId}/performance`
Aggregate performance summary (backed by `model_performance_summary`).

**Response 200**
```json
{
  "modelId": "b1e2c3d4-...",
  "modelName": "YOLOv8-Aerial",
  "totalEvaluations": 15230,
  "avgIou": 0.874,
  "accuracyRate": 0.912,
  "avgConfidence": 0.83,
  "avgLatencyMs": 42.7
}
```

### 4.3 `GET /api/v1/sessions/{sessionId}/frames?page=1&pageSize=50`
Paginated telemetry frames for a session (for replay view).

**Response 200**
```json
{
  "page": 1,
  "pageSize": 50,
  "totalCount": 3400,
  "items": [
    {
      "id": "f1a2...",
      "frameSequence": 101,
      "capturedAt": "2026-07-01T10:15:32.120Z",
      "altitudeM": 1450.2,
      "velocityMps": 210.5
    }
  ]
}
```

### 4.4 `GET /api/v1/frames/{frameId}/inferences`
All model inferences for a given frame, side-by-side (core comparison view).

**Response 200**
```json
[
  {
    "modelId": "b1e2c3d4-...",
    "modelName": "YOLOv8-Aerial",
    "detectedClass": "unmanned_vehicle",
    "confidenceScore": 0.91,
    "boundingBox": { "x": 120, "y": 84, "w": 60, "h": 40 },
    "inferenceLatencyMs": 38.2
  },
  {
    "modelId": "c2f3d4e5-...",
    "modelName": "RT-DETR-Tactical",
    "detectedClass": "unmanned_vehicle",
    "confidenceScore": 0.77,
    "boundingBox": { "x": 118, "y": 90, "w": 55, "h": 42 },
    "inferenceLatencyMs": 51.9
  }
]
```

### 4.5 `GET /api/v1/frames/{frameId}/drift`
Drift metrics for all model inferences against ground truth for that frame.

**Response 200**
```json
[
  {
    "modelId": "b1e2c3d4-...",
    "iouScore": 0.89,
    "confidenceDelta": 0.09,
    "classificationCorrect": true
  }
]
```

### 4.6 `POST /api/v1/ground-truths` *(used by simulation oracle, not the dashboard)*

**Request**
```json
{
  "frameId": "f1a2...",
  "verifiedBy": "simulation_oracle_v1",
  "verificationMethod": "simulation_oracle",
  "trueClass": "unmanned_vehicle",
  "boundingBox": { "x": 119, "y": 87, "w": 58, "h": 41 }
}
```

**Response 201**
```json
{ "id": "gt1a2..." }
```

### 4.7 Real-Time Channel — `SignalR Hub: /hubs/drift-dashboard`

**Server → Client event:** `DriftComputed`
```json
{
  "frameId": "f1a2...",
  "modelId": "b1e2c3d4-...",
  "modelName": "YOLOv8-Aerial",
  "iouScore": 0.89,
  "confidenceScore": 0.91,
  "classificationCorrect": true,
  "timestamp": "2026-07-01T10:15:32.980Z"
}
```

This is the endpoint the dashboard subscribes to for live-updating charts, rather than polling the REST endpoints above.

---

## 5. Points Worth Emphasizing in Your Interview / Submission

1. **Auditability over cleverness.** In a defense context, the reviewer is checking whether you understand that every prediction must be traceable back to raw sensor input and a verifiable ground truth — not just whether you can wire up RabbitMQ. The schema's separation of `raw_output` (jsonb) from computed `drift_metrics` exists specifically for this.
2. **Decoupled ingestion.** The mock producers never talk to PostgreSQL directly — everything passes through the broker. This is the detail that signals "I understand why real-time defense telemetry pipelines are architected this way," not just "I know how to use a queue."
3. **Read/write separation on the dashboard side.** SignalR for live push, REST + materialized view for historical/aggregate queries — avoids over-engineering (no need for a full CQRS/event-sourcing stack for a PoC) while still demonstrating the right instinct.

Good luck with Baykar — this is a strong scope for a new-grad portfolio piece. If you want, I can next help you scope down the **MVP cut** (which 20% of this you'd actually build in a limited timeframe to have something demoable, versus what stays diagram-only).
