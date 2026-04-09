using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthApi.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePatientIdentifier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeviceRegistrations_PatientId",
                table: "DeviceRegistrations");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "DeviceRegistrations");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                table: "DeviceRegistrations",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "PatientIdentifier",
                table: "DeviceRegistrations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceRegistrations_PatientIdentifier_DateOfBirth",
                table: "DeviceRegistrations",
                columns: new[] { "PatientIdentifier", "DateOfBirth" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeviceRegistrations_PatientIdentifier_DateOfBirth",
                table: "DeviceRegistrations");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "DeviceRegistrations");

            migrationBuilder.DropColumn(
                name: "PatientIdentifier",
                table: "DeviceRegistrations");

            migrationBuilder.AddColumn<int>(
                name: "PatientId",
                table: "DeviceRegistrations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceRegistrations_PatientId",
                table: "DeviceRegistrations",
                column: "PatientId");
        }
    }
}
