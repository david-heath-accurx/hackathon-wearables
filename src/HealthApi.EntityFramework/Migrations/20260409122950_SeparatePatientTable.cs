using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthApi.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class SeparatePatientTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create Patients table
            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientIdentifier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    RegisteredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_PatientIdentifier",
                table: "Patients",
                column: "PatientIdentifier",
                unique: true);

            // 2. Populate Patients from distinct (PatientIdentifier, DateOfBirth) in DeviceRegistrations
            migrationBuilder.Sql(@"
                INSERT INTO Patients (Id, PatientIdentifier, DateOfBirth, RegisteredAt)
                SELECT NEWID(), PatientIdentifier, DateOfBirth, MIN(RegisteredAt)
                FROM DeviceRegistrations
                GROUP BY PatientIdentifier, DateOfBirth
            ");

            // 3. Add PatientId column (nullable temporarily to allow data fill)
            migrationBuilder.AddColumn<Guid>(
                name: "PatientId",
                table: "DeviceRegistrations",
                type: "uniqueidentifier",
                nullable: true);

            // 4. Populate PatientId from Patients
            migrationBuilder.Sql(@"
                UPDATE dr
                SET dr.PatientId = p.Id
                FROM DeviceRegistrations dr
                JOIN Patients p ON p.PatientIdentifier = dr.PatientIdentifier
                                AND p.DateOfBirth = dr.DateOfBirth
            ");

            // 5. Make PatientId NOT NULL now that it is populated
            migrationBuilder.AlterColumn<Guid>(
                name: "PatientId",
                table: "DeviceRegistrations",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // 6. Add FK and index
            migrationBuilder.CreateIndex(
                name: "IX_DeviceRegistrations_PatientId",
                table: "DeviceRegistrations",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceRegistrations_Patients_PatientId",
                table: "DeviceRegistrations",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // 7. Drop old columns now that data is migrated
            migrationBuilder.DropIndex(
                name: "IX_DeviceRegistrations_PatientIdentifier_DateOfBirth",
                table: "DeviceRegistrations");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "DeviceRegistrations");

            migrationBuilder.DropColumn(
                name: "PatientIdentifier",
                table: "DeviceRegistrations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeviceRegistrations_Patients_PatientId",
                table: "DeviceRegistrations");

            migrationBuilder.DropTable(
                name: "Patients");

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
    }
}
