using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollCalculator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAgreementCodeAndOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Agreements",
                type: "TEXT",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Origin",
                table: "Agreements",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Agreements",
                type: "INTEGER",
                nullable: true);

            // Agreements entered before sprint 07 are the user's: give each a unique generated code before the unique index.
            migrationBuilder.Sql("UPDATE Agreements SET Code = 'custom_' || Id, Origin = 'User' WHERE Code = '';");

            migrationBuilder.CreateIndex(
                name: "IX_Agreements_Code",
                table: "Agreements",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Agreements_Code",
                table: "Agreements");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Agreements");

            migrationBuilder.DropColumn(
                name: "Origin",
                table: "Agreements");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Agreements");
        }
    }
}
