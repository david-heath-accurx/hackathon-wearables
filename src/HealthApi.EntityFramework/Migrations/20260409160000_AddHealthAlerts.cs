using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthApi.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HealthAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AcknowledgedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthAlerts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HealthAlerts_PatientIdentifier_DetectedAt",
                table: "HealthAlerts",
                columns: new[] { "PatientIdentifier", "DetectedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "HealthAlerts");
        }
    }
}
