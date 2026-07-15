using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiModelEvalCenter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDriftAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DriftAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    TriggeredAtIou = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    WindowSize = table.Column<int>(type: "integer", nullable: false),
                    Threshold = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriftAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriftAlerts_AiModels_ModelId",
                        column: x => x.ModelId,
                        principalTable: "AiModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriftAlerts_ModelId_DetectedAt",
                table: "DriftAlerts",
                columns: new[] { "ModelId", "DetectedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriftAlerts");
        }
    }
}
