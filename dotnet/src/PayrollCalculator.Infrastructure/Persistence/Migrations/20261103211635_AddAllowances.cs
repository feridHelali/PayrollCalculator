using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollCalculator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Allowances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AgreementId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AppliesTo = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Conditions = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Allowances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Allowances_Agreements_AgreementId",
                        column: x => x.AgreementId,
                        principalTable: "Agreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AllowanceRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EffectiveFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Value = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AllowanceId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllowanceRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllowanceRates_Allowances_AllowanceId",
                        column: x => x.AllowanceId,
                        principalTable: "Allowances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllowanceRates_AllowanceId",
                table: "AllowanceRates",
                column: "AllowanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Allowances_AgreementId",
                table: "Allowances",
                column: "AgreementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllowanceRates");

            migrationBuilder.DropTable(
                name: "Allowances");
        }
    }
}
