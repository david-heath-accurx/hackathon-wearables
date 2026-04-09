using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthApi.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddPracticeOdsCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add nullable first so existing rows aren't rejected, then default and make NOT NULL
            migrationBuilder.AddColumn<string>(
                name: "PracticeOdsCode",
                table: "Patients",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            // Existing patients have an unknown ODS code; use empty string as a placeholder
            migrationBuilder.Sql("UPDATE Patients SET PracticeOdsCode = '' WHERE PracticeOdsCode IS NULL");

            migrationBuilder.AlterColumn<string>(
                name: "PracticeOdsCode",
                table: "Patients",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PracticeOdsCode", table: "Patients");
        }
    }
}
