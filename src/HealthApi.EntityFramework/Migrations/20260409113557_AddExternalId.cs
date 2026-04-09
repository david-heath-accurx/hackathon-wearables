using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthApi.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "HealthDataPoints",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HealthDataPoints_DeviceId_ExternalId",
                table: "HealthDataPoints",
                columns: new[] { "DeviceId", "ExternalId" },
                unique: true,
                filter: "[ExternalId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HealthDataPoints_DeviceId_ExternalId",
                table: "HealthDataPoints");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "HealthDataPoints");
        }
    }
}
