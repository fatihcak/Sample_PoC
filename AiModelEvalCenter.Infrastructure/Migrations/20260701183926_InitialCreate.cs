using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AiModelEvalCenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModelType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Framework = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelemetrySessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AircraftId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SessionLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetrySessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelemetryFrames",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameSequence = table.Column<long>(type: "bigint", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AltitudeM = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    VelocityMps = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    HeadingDeg = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    SensorPayloadUri = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetryFrames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelemetryFrames_TelemetrySessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "TelemetrySessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroundTruths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerifiedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    VerificationMethod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TrueClass = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    BoundingBox = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroundTruths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroundTruths_TelemetryFrames_FrameId",
                        column: x => x.FrameId,
                        principalTable: "TelemetryFrames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModelInferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    DetectedClass = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    InferenceLatencyMs = table.Column<decimal>(type: "numeric(10,3)", nullable: true),
                    RawOutput = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    BoundingBox = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelInferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModelInferences_AiModels_ModelId",
                        column: x => x.ModelId,
                        principalTable: "AiModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModelInferences_TelemetryFrames_FrameId",
                        column: x => x.FrameId,
                        principalTable: "TelemetryFrames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriftMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroundTruthId = table.Column<Guid>(type: "uuid", nullable: false),
                    IouScore = table.Column<decimal>(type: "numeric(5,4)", nullable: true),
                    ConfidenceDelta = table.Column<decimal>(type: "numeric(5,4)", nullable: true),
                    ClassificationCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    ComputedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriftMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriftMetrics_GroundTruths_GroundTruthId",
                        column: x => x.GroundTruthId,
                        principalTable: "GroundTruths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DriftMetrics_ModelInferences_InferenceId",
                        column: x => x.InferenceId,
                        principalTable: "ModelInferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AiModels",
                columns: new[] { "Id", "CreatedAt", "Description", "Framework", "IsActive", "ModelType", "Name", "Version" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Mock Object Detection Model", "ONNX", true, "object_detection", "YOLOv8-Aerial", "2.3.1" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "High precision tactical model", "TensorRT", true, "object_detection", "RT-DETR-Tactical", "1.0.0" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Thermal segmentation model", "PyTorch", true, "segmentation", "Thermal-Seg", "4.0" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiModels_Name_Version",
                table: "AiModels",
                columns: new[] { "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriftMetrics_GroundTruthId",
                table: "DriftMetrics",
                column: "GroundTruthId");

            migrationBuilder.CreateIndex(
                name: "IX_DriftMetrics_InferenceId",
                table: "DriftMetrics",
                column: "InferenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroundTruths_FrameId",
                table: "GroundTruths",
                column: "FrameId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModelInferences_FrameId",
                table: "ModelInferences",
                column: "FrameId");

            migrationBuilder.CreateIndex(
                name: "IX_ModelInferences_ModelId_CreatedAt",
                table: "ModelInferences",
                columns: new[] { "ModelId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryFrames_SessionId_CapturedAt",
                table: "TelemetryFrames",
                columns: new[] { "SessionId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryFrames_SessionId_FrameSequence",
                table: "TelemetryFrames",
                columns: new[] { "SessionId", "FrameSequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriftMetrics");

            migrationBuilder.DropTable(
                name: "GroundTruths");

            migrationBuilder.DropTable(
                name: "ModelInferences");

            migrationBuilder.DropTable(
                name: "AiModels");

            migrationBuilder.DropTable(
                name: "TelemetryFrames");

            migrationBuilder.DropTable(
                name: "TelemetrySessions");
        }
    }
}
