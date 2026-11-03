using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollCalculator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Agreements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agreements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalaryTables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AgreementId = table.Column<int>(type: "INTEGER", nullable: false),
                    TableNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SalaryType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ConcernedEmployees = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ApplicationEnd = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ApplicationStart = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryTables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryTables_Agreements_AgreementId",
                        column: x => x.AgreementId,
                        principalTable: "Agreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Affairs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AffairNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Claimant = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    AgreementId = table.Column<int>(type: "INTEGER", nullable: false),
                    SalaryTableId = table.Column<int>(type: "INTEGER", nullable: true),
                    EmploymentEnd = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EmploymentStart = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    InitialCategory = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    InitialDegree = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Affairs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Affairs_Agreements_AgreementId",
                        column: x => x.AgreementId,
                        principalTable: "Agreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Affairs_SalaryTables_SalaryTableId",
                        column: x => x.SalaryTableId,
                        principalTable: "SalaryTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SalaryCells",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CategoryKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DegreeKey = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    SalaryTableId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryCells", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryCells_SalaryTables_SalaryTableId",
                        column: x => x.SalaryTableId,
                        principalTable: "SalaryTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalaryTableCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    SalaryTableId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryTableCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryTableCategories_SalaryTables_SalaryTableId",
                        column: x => x.SalaryTableId,
                        principalTable: "SalaryTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalaryTableDegrees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    YearsInDegree = table.Column<int>(type: "INTEGER", nullable: false),
                    SalaryTableId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryTableDegrees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryTableDegrees_SalaryTables_SalaryTableId",
                        column: x => x.SalaryTableId,
                        principalTable: "SalaryTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Affairs_AgreementId",
                table: "Affairs",
                column: "AgreementId");

            migrationBuilder.CreateIndex(
                name: "IX_Affairs_SalaryTableId",
                table: "Affairs",
                column: "SalaryTableId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryCells_SalaryTableId",
                table: "SalaryCells",
                column: "SalaryTableId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryTableCategories_SalaryTableId",
                table: "SalaryTableCategories",
                column: "SalaryTableId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryTableDegrees_SalaryTableId",
                table: "SalaryTableDegrees",
                column: "SalaryTableId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryTables_AgreementId_TableNumber",
                table: "SalaryTables",
                columns: new[] { "AgreementId", "TableNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Affairs");

            migrationBuilder.DropTable(
                name: "SalaryCells");

            migrationBuilder.DropTable(
                name: "SalaryTableCategories");

            migrationBuilder.DropTable(
                name: "SalaryTableDegrees");

            migrationBuilder.DropTable(
                name: "SalaryTables");

            migrationBuilder.DropTable(
                name: "Agreements");
        }
    }
}
