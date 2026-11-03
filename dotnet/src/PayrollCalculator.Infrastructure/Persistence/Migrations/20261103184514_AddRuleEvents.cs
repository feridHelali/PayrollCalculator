using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PayrollCalculator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRuleEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareerEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AffairId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareerEvents_Affairs_AffairId",
                        column: x => x.AffairId,
                        principalTable: "Affairs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LedgerSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AffairId = table.Column<int>(type: "INTEGER", nullable: false),
                    Purpose = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RuleEventIds = table.Column<string>(type: "TEXT", nullable: false),
                    CareerEvents = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TotalMillimes = table.Column<long>(type: "INTEGER", nullable: false),
                    Months = table.Column<int>(type: "INTEGER", nullable: false),
                    TakenBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TakenAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AbsenceSuspendsSeniority = table.Column<bool>(type: "INTEGER", nullable: false),
                    Proration = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ResetTimeInDegreeOnReclassification = table.Column<bool>(type: "INTEGER", nullable: false),
                    Rounding = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LedgerSnapshots_Affairs_AffairId",
                        column: x => x.AffairId,
                        principalTable: "Affairs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LegalSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Number = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    SignedOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    JortIssue = table.Column<int>(type: "INTEGER", nullable: true),
                    JortDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuleEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AgreementId = table.Column<int>(type: "INTEGER", nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    LegalSourceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RecordedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReviewedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleEvents_Agreements_AgreementId",
                        column: x => x.AgreementId,
                        principalTable: "Agreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RuleEvents_LegalSources_LegalSourceId",
                        column: x => x.LegalSourceId,
                        principalTable: "LegalSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareerEvents_AffairId",
                table: "CareerEvents",
                column: "AffairId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerEvents_EventId",
                table: "CareerEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LedgerSnapshots_AffairId",
                table: "LedgerSnapshots",
                column: "AffairId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalSources_Kind_Number",
                table: "LegalSources",
                columns: new[] { "Kind", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuleEvents_AgreementId_EffectiveFrom",
                table: "RuleEvents",
                columns: new[] { "AgreementId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_RuleEvents_LegalSourceId",
                table: "RuleEvents",
                column: "LegalSourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareerEvents");

            migrationBuilder.DropTable(
                name: "LedgerSnapshots");

            migrationBuilder.DropTable(
                name: "RuleEvents");

            migrationBuilder.DropTable(
                name: "LegalSources");
        }
    }
}
