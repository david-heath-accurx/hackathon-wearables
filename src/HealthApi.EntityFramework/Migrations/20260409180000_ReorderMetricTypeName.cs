using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthApi.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class ReorderMetricTypeName : Migration
    {
        // The MetricTypeName computed column definition, used in both Up and Down
        private const string MetricTypeNameSql = """
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
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server doesn't support reordering columns in place, so recreate the table
            // with MetricTypeName placed immediately after MetricType.
            migrationBuilder.Sql("""
                -- Step 1: Create replacement table with desired column order
                CREATE TABLE [HealthDataPoints_New] (
                    [Id]                     uniqueidentifier  NOT NULL DEFAULT NEWSEQUENTIALID(),
                    [DeviceRegistrationId]   uniqueidentifier  NOT NULL,
                    [MetricType]             int               NOT NULL,
                    [MetricTypeName]         AS (CASE [MetricType]
                                                  WHEN 0  THEN N'HeartRate'
                                                  WHEN 1  THEN N'Steps'
                                                  WHEN 2  THEN N'ActiveCalories'
                                                  WHEN 3  THEN N'RestingCalories'
                                                  WHEN 4  THEN N'BloodOxygen'
                                                  WHEN 5  THEN N'SleepDuration'
                                                  WHEN 6  THEN N'StandHours'
                                                  WHEN 7  THEN N'ExerciseMinutes'
                                                  WHEN 8  THEN N'WorkoutDuration'
                                                  WHEN 9  THEN N'RespiratoryRate'
                                                  WHEN 10 THEN N'HeartRateVariability'
                                                  ELSE CAST([MetricType] AS NVARCHAR(50))
                                                END) PERSISTED,
                    [Value]                  float             NOT NULL,
                    [Unit]                   nvarchar(50)      NOT NULL,
                    [RecordedAt]             datetimeoffset    NOT NULL,
                    [ExternalId]             nvarchar(256)     NOT NULL,
                    [CreatedAt]              datetimeoffset    NOT NULL,
                    CONSTRAINT [PK_HealthDataPoints_New] PRIMARY KEY ([Id])
                );

                -- Step 2: Copy all data
                INSERT INTO [HealthDataPoints_New]
                    ([Id], [DeviceRegistrationId], [MetricType], [Value], [Unit], [RecordedAt], [ExternalId], [CreatedAt])
                SELECT [Id], [DeviceRegistrationId], [MetricType], [Value], [Unit], [RecordedAt], [ExternalId], [CreatedAt]
                FROM [HealthDataPoints];

                -- Step 3: Drop FK, indexes, and old table
                ALTER TABLE [HealthDataPoints] DROP CONSTRAINT [FK_HealthDataPoints_DeviceRegistrations_DeviceRegistrationId];
                DROP INDEX [IX_HealthDataPoints_DeviceRegistrationId_ExternalId]       ON [HealthDataPoints];
                DROP INDEX [IX_HealthDataPoints_DeviceRegistrationId_MetricType_RecordedAt] ON [HealthDataPoints];
                DROP TABLE [HealthDataPoints];

                -- Step 4: Rename new table and recreate constraints / indexes
                EXEC sp_rename 'HealthDataPoints_New',                                    'HealthDataPoints';
                EXEC sp_rename 'PK_HealthDataPoints_New',                                 'PK_HealthDataPoints',   'OBJECT';

                ALTER TABLE [HealthDataPoints]
                    ADD CONSTRAINT [FK_HealthDataPoints_DeviceRegistrations_DeviceRegistrationId]
                    FOREIGN KEY ([DeviceRegistrationId]) REFERENCES [DeviceRegistrations] ([Id])
                    ON DELETE CASCADE;

                CREATE INDEX [IX_HealthDataPoints_DeviceRegistrationId_MetricType_RecordedAt]
                    ON [HealthDataPoints] ([DeviceRegistrationId], [MetricType], [RecordedAt]);

                CREATE UNIQUE INDEX [IX_HealthDataPoints_DeviceRegistrationId_ExternalId]
                    ON [HealthDataPoints] ([DeviceRegistrationId], [ExternalId]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Move MetricTypeName back to after CreatedAt (original position from previous migration)
            migrationBuilder.Sql("""
                CREATE TABLE [HealthDataPoints_Old] (
                    [Id]                     uniqueidentifier  NOT NULL DEFAULT NEWSEQUENTIALID(),
                    [DeviceRegistrationId]   uniqueidentifier  NOT NULL,
                    [MetricType]             int               NOT NULL,
                    [Value]                  float             NOT NULL,
                    [Unit]                   nvarchar(50)      NOT NULL,
                    [RecordedAt]             datetimeoffset    NOT NULL,
                    [ExternalId]             nvarchar(256)     NOT NULL,
                    [CreatedAt]              datetimeoffset    NOT NULL,
                    [MetricTypeName]         AS (CASE [MetricType]
                                                  WHEN 0  THEN N'HeartRate'
                                                  WHEN 1  THEN N'Steps'
                                                  WHEN 2  THEN N'ActiveCalories'
                                                  WHEN 3  THEN N'RestingCalories'
                                                  WHEN 4  THEN N'BloodOxygen'
                                                  WHEN 5  THEN N'SleepDuration'
                                                  WHEN 6  THEN N'StandHours'
                                                  WHEN 7  THEN N'ExerciseMinutes'
                                                  WHEN 8  THEN N'WorkoutDuration'
                                                  WHEN 9  THEN N'RespiratoryRate'
                                                  WHEN 10 THEN N'HeartRateVariability'
                                                  ELSE CAST([MetricType] AS NVARCHAR(50))
                                                END) PERSISTED,
                    CONSTRAINT [PK_HealthDataPoints_Old] PRIMARY KEY ([Id])
                );

                INSERT INTO [HealthDataPoints_Old]
                    ([Id], [DeviceRegistrationId], [MetricType], [Value], [Unit], [RecordedAt], [ExternalId], [CreatedAt])
                SELECT [Id], [DeviceRegistrationId], [MetricType], [Value], [Unit], [RecordedAt], [ExternalId], [CreatedAt]
                FROM [HealthDataPoints];

                ALTER TABLE [HealthDataPoints] DROP CONSTRAINT [FK_HealthDataPoints_DeviceRegistrations_DeviceRegistrationId];
                DROP INDEX [IX_HealthDataPoints_DeviceRegistrationId_ExternalId]           ON [HealthDataPoints];
                DROP INDEX [IX_HealthDataPoints_DeviceRegistrationId_MetricType_RecordedAt] ON [HealthDataPoints];
                DROP TABLE [HealthDataPoints];

                EXEC sp_rename 'HealthDataPoints_Old', 'HealthDataPoints';
                EXEC sp_rename 'PK_HealthDataPoints_Old', 'PK_HealthDataPoints', 'OBJECT';

                ALTER TABLE [HealthDataPoints]
                    ADD CONSTRAINT [FK_HealthDataPoints_DeviceRegistrations_DeviceRegistrationId]
                    FOREIGN KEY ([DeviceRegistrationId]) REFERENCES [DeviceRegistrations] ([Id])
                    ON DELETE CASCADE;

                CREATE INDEX [IX_HealthDataPoints_DeviceRegistrationId_MetricType_RecordedAt]
                    ON [HealthDataPoints] ([DeviceRegistrationId], [MetricType], [RecordedAt]);

                CREATE UNIQUE INDEX [IX_HealthDataPoints_DeviceRegistrationId_ExternalId]
                    ON [HealthDataPoints] ([DeviceRegistrationId], [ExternalId]);
                """);
        }
    }
}
