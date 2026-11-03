using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollCalculator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaidSalary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AffairPaidSalary",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    From = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AffairId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AffairPaidSalary", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AffairPaidSalary_Affairs_AffairId",
                        column: x => x.AffairId,
                        principalTable: "Affairs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AffairPaidSalary_AffairId",
                table: "AffairPaidSalary",
                column: "AffairId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AffairPaidSalary");
        }
    }
}
