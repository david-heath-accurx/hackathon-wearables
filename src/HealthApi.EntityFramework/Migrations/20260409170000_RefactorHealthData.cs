using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthApi.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class RefactorHealthData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── DeviceRegistrations: add DeviceModel ────────────────────────────────

            migrationBuilder.AddColumn<string>(
                name: "DeviceModel",
                table: "DeviceRegistrations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            // Populate DeviceModel from the most recent health data point for each device
            migrationBuilder.Sql("""
                UPDATE dr
                SET dr.DeviceModel = (
                    SELECT TOP 1 hdp.DeviceModel
                    FROM HealthDataPoints hdp
                    WHERE hdp.DeviceId = dr.DeviceId
                      AND hdp.DeviceModel IS NOT NULL
                    ORDER BY hdp.CreatedAt DESC
                )
                FROM DeviceRegistrations dr
                """);

            // ── HealthDataPoints: add DeviceRegistrationId ──────────────────────────

            migrationBuilder.AddColumn<Guid>(
                name: "DeviceRegistrationId",
                table: "HealthDataPoints",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE hdp
                SET hdp.DeviceRegistrationId = dr.Id
                FROM HealthDataPoints hdp
                JOIN DeviceRegistrations dr ON dr.DeviceId = hdp.DeviceId
                """);

            // Remove any orphaned rows that couldn't be matched to a registration
            migrationBuilder.Sql("""
                DELETE FROM HealthDataPoints WHERE DeviceRegistrationId IS NULL
                """);

            // Generate ExternalIds for any rows that don't have one
            migrationBuilder.Sql("""
                UPDATE HealthDataPoints
                SET ExternalId = CAST(NEWID() AS NVARCHAR(256))
                WHERE ExternalId IS NULL
                """);

            // Drop old indexes before altering columns they reference
            migrationBuilder.DropIndex(
                name: "IX_HealthDataPoints_DeviceId_ExternalId",
                table: "HealthDataPoints");

            migrationBuilder.DropIndex(
                name: "IX_HealthDataPoints_UserId_MetricType_RecordedAt",
                table: "HealthDataPoints");

            // Make both new columns NOT NULL now that data is populated
            migrationBuilder.AlterColumn<Guid>(
                name: "DeviceRegistrationId",
                table: "HealthDataPoints",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ExternalId",
                table: "HealthDataPoints",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            // Drop old columns from HealthDataPoints
            migrationBuilder.DropColumn(name: "UserId", table: "HealthDataPoints");
            migrationBuilder.DropColumn(name: "DeviceId", table: "HealthDataPoints");
            migrationBuilder.DropColumn(name: "DeviceModel", table: "HealthDataPoints");

            // Add FK, new indexes, and computed column
            migrationBuilder.CreateIndex(
                name: "IX_HealthDataPoints_DeviceRegistrationId_MetricType_RecordedAt",
                table: "HealthDataPoints",
                columns: ["DeviceRegistrationId", "MetricType", "RecordedAt"]);

            migrationBuilder.CreateIndex(
                name: "IX_HealthDataPoints_DeviceRegistrationId_ExternalId",
                table: "HealthDataPoints",
                columns: ["DeviceRegistrationId", "ExternalId"],
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_HealthDataPoints_DeviceRegistrations_DeviceRegistrationId",
                table: "HealthDataPoints",
                column: "DeviceRegistrationId",
                principalTable: "DeviceRegistrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql("""
                ALTER TABLE HealthDataPoints ADD MetricTypeName AS (
                    CASE [MetricType]
                      WHEN 0 THEN N'HeartRate'
                      WHEN 1 THEN N'Steps'
                      WHEN 2 THEN N'ActiveCalories'
                      WHEN 3 THEN N'RestingCalories'
                      WHEN 4 THEN N'BloodOxygen'
                      WHEN 5 THEN N'SleepDuration'
                      WHEN 6 THEN N'StandHours'
                      WHEN 7 THEN N'ExerciseMinutes'
                      WHEN 8 THEN N'WorkoutDuration'
                      WHEN 9 THEN N'RespiratoryRate'
                      WHEN 10 THEN N'HeartRateVariability'
                      ELSE CAST([MetricType] AS NVARCHAR(50))
                    END
                ) PERSISTED
                """);

            // ── HealthAlerts: replace PatientIdentifier with PatientId FK ───────────

            migrationBuilder.AddColumn<Guid>(
                name: "PatientId",
                table: "HealthAlerts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE ha
                SET ha.PatientId = p.Id
                FROM HealthAlerts ha
                JOIN Patients p ON p.PatientIdentifier = ha.PatientIdentifier
                """);

            // Remove alerts whose patient is no longer found (defensive)
            migrationBuilder.Sql("""
                DELETE FROM HealthAlerts WHERE PatientId IS NULL
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "PatientId",
                table: "HealthAlerts",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_HealthAlerts_PatientIdentifier_DetectedAt",
                table: "HealthAlerts");

            migrationBuilder.DropColumn(name: "PatientIdentifier", table: "HealthAlerts");

            migrationBuilder.CreateIndex(
                name: "IX_HealthAlerts_PatientId_DetectedAt",
                table: "HealthAlerts",
                columns: ["PatientId", "DetectedAt"]);

            migrationBuilder.AddForeignKey(
                name: "FK_HealthAlerts_Patients_PatientId",
                table: "HealthAlerts",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ── Reverse HealthAlerts changes ─────────────────────────────────────────

            migrationBuilder.DropForeignKey(
                name: "FK_HealthAlerts_Patients_PatientId",
                table: "HealthAlerts");

            migrationBuilder.DropIndex(
                name: "IX_HealthAlerts_PatientId_DetectedAt",
                table: "HealthAlerts");

            migrationBuilder.AddColumn<string>(
                name: "PatientIdentifier",
                table: "HealthAlerts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE ha
                SET ha.PatientIdentifier = p.PatientIdentifier
                FROM HealthAlerts ha
                JOIN Patients p ON p.Id = ha.PatientId
                """);

            migrationBuilder.AlterColumn<string>(
                name: "PatientIdentifier",
                table: "HealthAlerts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldNullable: true);

            migrationBuilder.DropColumn(name: "PatientId", table: "HealthAlerts");

            migrationBuilder.CreateIndex(
                name: "IX_HealthAlerts_PatientIdentifier_DetectedAt",
                table: "HealthAlerts",
                columns: ["PatientIdentifier", "DetectedAt"]);

            // ── Reverse HealthDataPoints changes ─────────────────────────────────────

            migrationBuilder.DropForeignKey(
                name: "FK_HealthDataPoints_DeviceRegistrations_DeviceRegistrationId",
                table: "HealthDataPoints");

            migrationBuilder.DropIndex(
                name: "IX_HealthDataPoints_DeviceRegistrationId_ExternalId",
                table: "HealthDataPoints");

            migrationBuilder.DropIndex(
                name: "IX_HealthDataPoints_DeviceRegistrationId_MetricType_RecordedAt",
                table: "HealthDataPoints");

            migrationBuilder.Sql("ALTER TABLE HealthDataPoints DROP COLUMN MetricTypeName");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "HealthDataPoints",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "HealthDataPoints",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceModel",
                table: "HealthDataPoints",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE hdp
                SET hdp.DeviceId = dr.DeviceId,
                    hdp.DeviceModel = dr.DeviceModel,
                    hdp.UserId = p.PatientIdentifier
                FROM HealthDataPoints hdp
                JOIN DeviceRegistrations dr ON dr.Id = hdp.DeviceRegistrationId
                JOIN Patients p ON p.Id = dr.PatientId
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ExternalId",
                table: "HealthDataPoints",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: false);

            migrationBuilder.DropColumn(name: "DeviceRegistrationId", table: "HealthDataPoints");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "HealthDataPoints",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HealthDataPoints_UserId_MetricType_RecordedAt",
                table: "HealthDataPoints",
                columns: ["UserId", "MetricType", "RecordedAt"]);

            migrationBuilder.CreateIndex(
                name: "IX_HealthDataPoints_DeviceId_ExternalId",
                table: "HealthDataPoints",
                columns: ["DeviceId", "ExternalId"],
                unique: true,
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.DropColumn(name: "DeviceModel", table: "DeviceRegistrations");
        }
    }
}
