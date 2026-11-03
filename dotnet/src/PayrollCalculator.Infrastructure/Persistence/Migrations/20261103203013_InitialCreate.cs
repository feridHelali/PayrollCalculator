using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

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
                    Code = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Origin = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agreements", x => x.Id);
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
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MustChangePassword = table.Column<bool>(type: "INTEGER", nullable: false),
                    FailedAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    LockedUntil = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastSignedInOn = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgreementGridKinds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    DefaultWeeklyHours = table.Column<int>(type: "INTEGER", nullable: false),
                    AgreementId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgreementGridKinds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgreementGridKinds_Agreements_AgreementId",
                        column: x => x.AgreementId,
                        principalTable: "Agreements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.InsertData(
                table: "Agreements",
                columns: new[] { "Id", "Code", "Description", "Name", "Origin", "SortOrder" },
                values: new object[,]
                {
                    { 1, "vetements_tisses_et_prets_a_porter", null, "الاتفاقية المشتركة القطاعية لصناعة الملابس المنسوجة والجاهزة", "Seed", 1 },
                    { 2, "textile", null, "الاتفاقية المشتركة القطاعية للنسيج", "Seed", 2 },
                    { 3, "batiment_et_travaux_publics", null, "الاتفاقية المشتركة القطاعية للبناء والأشغال العامة", "Seed", 3 },
                    { 4, "transformation_du_plastique", null, "الاتفاقية المشتركة القطاعية لصناعة تحويل البلاستيك", "Seed", 4 },
                    { 5, "banques_et_institutions_financieres", null, "الاتفاقية المشتركة القطاعية للبنوك والمؤسسات المالية", "Seed", 5 },
                    { 6, "agences_de_voyages", null, "الاتفاقية المشتركة القطاعية لوكالات الأسفار", "Seed", 6 },
                    { 7, "employes_des_pharmacies", null, "الاتفاقية المشتركة القطاعية لمستخدمي الصيدليات", "Seed", 7 },
                    { 8, "enseignement_prive", null, "الاتفاقية المشتركة القطاعية للتعليم الخاص", "Seed", 8 },
                    { 9, "hotels_touristiques", null, "الاتفاقية المشتركة القطاعية للنزل السياحية والمؤسسات المشابهة", "Seed", 9 },
                    { 10, "assurances", null, "الاتفاقية المشتركة القطاعية للتأمين", "Seed", 10 },
                    { 11, "fonderie_metallurgie_mecanique", null, "الاتفاقية المشتركة القطاعية للمسابك والتعدين والبناءات الميكانيكية", "Seed", 11 },
                    { 12, "electricite_et_electronique", null, "الاتفاقية المشتركة القطاعية لصناعة الكهرباء والألكترونيك", "Seed", 12 },
                    { 13, "construction_metallique", null, "الاتفاقية المشتركة القطاعية للبناء المعدني", "Seed", 13 },
                    { 14, "constructeurs_et_concessionnaires_automobiles", null, "الاتفاقية المشتركة القطاعية لصانعي ووكلاء السيارات", "Seed", 14 },
                    { 15, "equipements_agricoles_et_genie_civil", null, "الاتفاقية المشتركة القطاعية لوكلاء التجهيزات الفلاحية والهندسة المدنية", "Seed", 15 },
                    { 16, "cliniques_privees", null, "الاتفاقية المشتركة القطاعية للمصحات الخاصة", "Seed", 16 },
                    { 17, "gestion_des_dechets", null, "الاتفاقية المشتركة القطاعية للتصرف في النفايات الصلبة والسائلة", "Seed", 17 },
                    { 18, "transformation_du_verre_et_miroiterie", null, "الاتفاقية المشتركة القطاعية لتحويل البلور والمرآة", "Seed", 18 },
                    { 19, "commerce_et_distribution", null, "الاتفاقية المشتركة القطاعية للتجارة وتوزيع المواد والمنتوجات", "Seed", 19 },
                    { 20, "explosifs_et_pyrotechnie", null, "الاتفاقية المشتركة القطاعية للمفرقعات", "Seed", 20 }
                });

            migrationBuilder.InsertData(
                table: "AgreementGridKinds",
                columns: new[] { "Id", "AgreementId", "Code", "DefaultWeeklyHours", "Name", "Unit" },
                values: new object[,]
                {
                    { 1, 1, "mensuel_cadres_maitrise", 48, "جدول الأجور الخاص بالإطارات وأعوان التسيير (شهري)", "Monthly" },
                    { 2, 1, "horaire_executants", 48, "جدول الأجور الخاص بأعوان التنفيذ (ساعي)", "Hourly" },
                    { 3, 2, "mensuel_cadres_maitrise", 48, "جدول الأجور الخاص بالإطارات وأعوان التسيير", "Monthly" },
                    { 4, 2, "horaire_ouvriers", 48, "جدول الأجور الخاص بالعملة (ساعي)", "Hourly" },
                    { 5, 3, "mensuel_administratif_technique", 48, "جدول أجور الأعوان الإداريين والفنيين والإطارات", "Monthly" },
                    { 6, 3, "horaire_ouvriers_qualifies", 48, "جدول أجور عملة التنفيذ (ساعي)", "Hourly" },
                    { 7, 4, "mensuel_cadres_maitrise", 48, "جدول الأجور الخاص بالإطارات وأعوان التسيير", "Monthly" },
                    { 8, 4, "horaire_executants", 48, "جدول أجور أعوان التنفيذ والعملة", "Hourly" },
                    { 9, 5, "mensuel_agents_titulaires", 40, "جدول الأجور الموحد لأعوان البنوك والمؤسسات المالية (شهري)", "Monthly" },
                    { 10, 6, "mensuel_toutes_categories", 48, "جدول الأجور الأساسية الموحد لجميع الأصناف", "Monthly" },
                    { 11, 7, "mensuel_diplomes_et_cadres", 40, "جدول أجور الصيادلة المساعدين والإطارات", "Monthly" },
                    { 12, 7, "mensuel_preparateurs_et_employes", 40, "جدول أجور المحضرين وأعوان الصيدليات", "Monthly" },
                    { 13, 8, "mensuel_enseignants", 40, "جدول أجور سلك التدريس والتعليم", "Monthly" },
                    { 14, 8, "mensuel_administratif", 40, "جدول أجور السلك الإداري وأعوان الإحاطة", "Monthly" },
                    { 15, 9, "mensuel_cadres_maitrise", 48, "جدول أجور إطارات وأعوان تسيير المؤسسات السياحية", "Monthly" },
                    { 16, 9, "mensuel_horaire_execution", 48, "جدول أجور أعوان التنفيذ (حسب نظام العمل)", "Monthly" },
                    { 17, 10, "mensuel_toutes_categories", 40, "جدول الأجور الأساسية الموحد لأعوان شركات التأمين", "Monthly" },
                    { 18, 11, "mensuel_cadres_maitrise", 48, "جدول أجور الإطارات وأعوان التسيير (المعادن)", "Monthly" },
                    { 19, 11, "horaire_ouvriers", 48, "جدول أجور عملة التنفيذ بسلك التعدين (ساعي)", "Hourly" },
                    { 20, 12, "mensuel_cadres_maitrise", 48, "جدول الأجور الخاص بالإطارات وأعوان التسيير", "Monthly" },
                    { 21, 12, "horaire_executants", 48, "جدول أجور أعوان التنفيذ والعملة (ساعي)", "Hourly" },
                    { 22, 13, "mensuel_cadres_maitrise", 48, "جدول الأجور الخاص بالإطارات وأعوان التسيير", "Monthly" },
                    { 23, 13, "horaire_ouvriers", 48, "جدول أجور عملة التنفيذ (ساعي)", "Hourly" },
                    { 24, 14, "mensuel_cadres_maitrise", 48, "جدول الأجور الخاص بإطارات وأعوان تسيير قطاع السيارات", "Monthly" },
                    { 25, 14, "horaire_ouvriers", 48, "جدول أجور أعوان التنفيذ والعملة (ساعي)", "Hourly" },
                    { 26, 15, "mensuel_cadres_maitrise", 48, "جدول الأجور الخاص بالإطارات وأعوان التسيير", "Monthly" },
                    { 27, 15, "horaire_executants", 48, "جدول أجور عملة وأعوان التنفيذ (ساعي)", "Hourly" },
                    { 28, 16, "mensuel_cadres_paramedical", 40, "جدول أجور الإطارات الطبية الموازية والإداريين", "Monthly" },
                    { 29, 16, "mensuel_execution_ouvriers", 40, "جدول أجور أعوان التنفيذ والعملة بالمصحات", "Monthly" },
                    { 30, 17, "mensuel_cadres_maitrise", 48, "جدول أجور سلك الإطارات وأعوان الإشراف", "Monthly" },
                    { 31, 17, "horaire_ouvriers_execution", 48, "جدول أجور عملة التنفيذ الميدانيين (ساعي)", "Hourly" },
                    { 32, 18, "mensuel_cadres_maitrise", 48, "جدول أجور إطارات وأعوان تسيير قطاع البلور", "Monthly" },
                    { 33, 18, "horaire_ouvriers", 48, "جدول أجور عملة الفروع المهنية للتنفيذ (ساعي)", "Hourly" },
                    { 34, 19, "mensuel_cadres_maitrise", 48, "جدول الأجور الخاص بالإطارات وأعوان التسيير والتجارة", "Monthly" },
                    { 35, 19, "mensuel_execution", 48, "جدول الأجور الخاص بأعوان التنفيذ والخدمات", "Monthly" },
                    { 36, 20, "mensuel_cadres_maitrise", 48, "جدول أجور الإطارات الفنية والإدارية", "Monthly" },
                    { 37, 20, "horaire_ouvriers_depots", 48, "جدول أجور عملة ومناولى المواد المتفجرة (ساعي)", "Hourly" }
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
                name: "IX_AgreementGridKinds_AgreementId_Code",
                table: "AgreementGridKinds",
                columns: new[] { "AgreementId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agreements_Code",
                table: "Agreements",
                column: "Code",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgreementGridKinds");

            migrationBuilder.DropTable(
                name: "CareerEvents");

            migrationBuilder.DropTable(
                name: "LedgerSnapshots");

            migrationBuilder.DropTable(
                name: "RuleEvents");

            migrationBuilder.DropTable(
                name: "SalaryCells");

            migrationBuilder.DropTable(
                name: "SalaryTableCategories");

            migrationBuilder.DropTable(
                name: "SalaryTableDegrees");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Affairs");

            migrationBuilder.DropTable(
                name: "LegalSources");

            migrationBuilder.DropTable(
                name: "SalaryTables");

            migrationBuilder.DropTable(
                name: "Agreements");
        }
    }
}
