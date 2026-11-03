using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Application.Agreements;
using System.IO;
using PayrollCalculator.Domain.Calculation;
using PayrollCalculator.Domain.SalaryTables;
using PayrollCalculator.Wpf.Services;
using PayrollCalculator.Wpf.ViewModels;

namespace PayrollCalculator.Wpf.Tests;

public sealed class ViewModelTests : IDisposable
{
    private readonly TestApp _app = new();

    public void Dispose() => _app.Dispose();

    private async Task<AgreementsViewModel> AgreementsWithOneAsync(string name = "المخابز")
    {
        var vm = _app.Get<AgreementsViewModel>();
        await vm.LoadAsync();
        vm.NewCommand.Execute(null);
        vm.EditName = name;
        await vm.SaveCommand.ExecuteAsync(null);
        return vm;
    }

    private async Task<SalaryTableEditorViewModel> SavedTableAsync()
    {
        await AgreementsWithOneAsync();
        var editor = _app.Get<SalaryTableEditorViewModel>();
        await editor.LoadAsync(null);
        editor.SelectedAgreementId = editor.Agreements[0].Id;
        editor.TableNumber = "4";
        editor.SalaryType = SalaryType.Monthly;
        editor.StartDate = new DateTime(2000, 1, 1);
        editor.AddCategoryCommand.Execute(null);
        editor.AddDegreeCommand.Execute(null);
        editor.AddDegreeCommand.Execute(null);
        editor.Categories[0].Label = "C1";
        editor.Degrees[0].YearsInDegree = 1;
        editor.Categories[0][editor.Degrees[0].Key] = 300m;
        editor.Categories[0][editor.Degrees[1].Key] = 350m;
        await editor.SaveCommand.ExecuteAsync(null);
        return editor;
    }

    /// <summary>The agreements entered by the user; the 20 seeded ones come first in the list.</summary>
    private static List<AgreementDto> UserAgreements(AgreementsViewModel vm) => vm.Agreements.Where(a => !a.IsSeeded).ToList();

    [Fact]
    public async Task Agreements_create_edit_and_delete()
    {
        var vm = await AgreementsWithOneAsync("A");
        Assert.Equal("A", Assert.Single(UserAgreements(vm)).Name);

        vm.SelectedAgreement = UserAgreements(vm)[0];
        Assert.Equal("A", vm.EditName);
        vm.EditName = "B";
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal("B", Assert.Single(UserAgreements(vm)).Name);

        vm.SelectedAgreement = UserAgreements(vm)[0];
        await vm.DeleteCommand.ExecuteAsync(null);
        Assert.Empty(UserAgreements(vm));
        Assert.Equal(20, vm.Agreements.Count);
    }

    [Fact]
    public async Task Delete_is_cancelled_when_not_confirmed()
    {
        var vm = await AgreementsWithOneAsync();
        _app.Dialogs.ConfirmAnswer = false;
        vm.SelectedAgreement = UserAgreements(vm)[0];

        await vm.DeleteCommand.ExecuteAsync(null);

        Assert.Single(UserAgreements(vm));
    }

    [Fact]
    public async Task A_seeded_agreement_cannot_be_deleted_and_the_error_is_shown()
    {
        var vm = _app.Get<AgreementsViewModel>();
        await vm.LoadAsync();
        vm.SelectedAgreement = vm.Agreements[0];

        await vm.DeleteCommand.ExecuteAsync(null);

        Assert.Single(_app.Dialogs.Errors);
        Assert.Equal(20, vm.Agreements.Count);
    }

    private async Task<SalaryTableEditorViewModel> EditorOnAsync(string agreementCode)
    {
        var editor = _app.Get<SalaryTableEditorViewModel>();
        await editor.LoadAsync(null);
        editor.SelectedAgreementId = editor.Agreements.Single(a => a.Code == agreementCode).Id;
        return editor;
    }

    [Fact]
    public async Task Choosing_an_agreement_lists_its_grid_kinds_and_a_kind_fills_the_type_and_employees()
    {
        var editor = await EditorOnAsync("textile");

        Assert.True(editor.HasGridKinds);
        Assert.Equal(["mensuel_cadres_maitrise", "horaire_ouvriers"], editor.GridKinds.Select(k => k.Code));

        editor.SelectedGridKind = editor.GridKinds[1];
        Assert.Equal((SalaryType.Hourly, "جدول الأجور الخاص بالعملة (ساعي)"), (editor.SalaryType, editor.ConcernedEmployees));
        Assert.Equal("48 ساعة في الأسبوع", editor.DefaultRegimeText);

        editor.SelectedGridKind = editor.GridKinds[0];
        Assert.Equal((SalaryType.Monthly, "جدول الأجور الخاص بالإطارات وأعوان التسيير"), (editor.SalaryType, editor.ConcernedEmployees));

        editor.SelectedAgreementId = editor.Agreements.Single(a => a.Code == "banques_et_institutions_financieres").Id;
        Assert.Single(editor.GridKinds);
        Assert.Null(editor.SelectedGridKind);
    }

    [Fact]
    public async Task A_grid_kind_does_not_overwrite_employees_typed_by_the_user()
    {
        var editor = await EditorOnAsync("textile");
        editor.ConcernedEmployees = "عملة الحياكة";

        editor.SelectedGridKind = editor.GridKinds[1];

        Assert.Equal((SalaryType.Hourly, "عملة الحياكة"), (editor.SalaryType, editor.ConcernedEmployees));
    }

    [Fact]
    public async Task A_saved_table_reopens_with_its_grid_kind_selected()
    {
        var editor = await EditorOnAsync("textile");
        editor.SelectedGridKind = editor.GridKinds[1];
        editor.TableNumber = "1";
        editor.StartDate = new DateTime(2022, 6, 1);
        await editor.SaveCommand.ExecuteAsync(null);

        var reopened = _app.Get<SalaryTableEditorViewModel>();
        await reopened.LoadAsync(editor.Id);

        Assert.Equal("horaire_ouvriers", reopened.SelectedGridKind?.Code);
        Assert.Equal(SalaryType.Hourly, reopened.SalaryType);
    }

    [Fact]
    public async Task The_trash_button_of_a_row_deletes_that_row_after_confirmation()
    {
        var vm = await AgreementsWithOneAsync("A");
        var row = UserAgreements(vm)[0];
        vm.SelectedAgreement = vm.Agreements[0]; // another row is selected: the row's own button wins

        _app.Dialogs.ConfirmAnswer = false;
        await vm.DeleteCommand.ExecuteAsync(row);
        Assert.Single(UserAgreements(vm));

        _app.Dialogs.ConfirmAnswer = true;
        await vm.DeleteCommand.ExecuteAsync(row);
        Assert.Empty(UserAgreements(vm));
        Assert.Equal(20, vm.Agreements.Count);
    }

    [Fact]
    public async Task Removing_a_category_or_a_degree_asks_first()
    {
        var editor = await EditorOnAsync("textile");
        editor.AddCategoryCommand.Execute(null);
        editor.AddDegreeCommand.Execute(null);

        _app.Dialogs.ConfirmAnswer = false;
        editor.RemoveCategoryCommand.Execute(editor.Categories[0]);
        editor.RemoveDegreeCommand.Execute(editor.Degrees[0]);
        Assert.Equal((1, 1), (editor.Categories.Count, editor.Degrees.Count));

        _app.Dialogs.ConfirmAnswer = true;
        editor.RemoveCategoryCommand.Execute(editor.Categories[0]);
        editor.RemoveDegreeCommand.Execute(editor.Degrees[0]);
        Assert.Equal((0, 0), (editor.Categories.Count, editor.Degrees.Count));
    }

    [Fact]
    public async Task Saving_a_salary_table_goes_back_to_the_list()
    {
        var shell = _app.Get<MainViewModel>();
        var editor = await EditorOnAsync("textile");
        editor.SelectedGridKind = editor.GridKinds[0];
        editor.TableNumber = "1";
        editor.StartDate = new DateTime(2022, 6, 1);

        await editor.SaveCommand.ExecuteAsync(null);

        var list = Assert.IsType<SalaryTablesViewModel>(shell.CurrentPage);
        Assert.Contains(list.Tables, t => t.Id == editor.Id);
    }

    [Fact]
    public async Task A_save_that_fails_stays_on_the_form()
    {
        var shell = _app.Get<MainViewModel>();
        var editor = await EditorOnAsync("textile");
        editor.StartDate = new DateTime(2022, 6, 1); // no table number: the domain refuses

        await editor.SaveCommand.ExecuteAsync(null);

        Assert.Single(_app.Dialogs.Errors);
        Assert.IsNotType<SalaryTablesViewModel>(shell.CurrentPage);
    }

    private static string Draft(string file, [System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", "data", "seed", "drafts",
            "vetements_tisses_et_prets_a_porter", file));

    [Fact]
    public async Task The_rider_18_monthly_draft_imports_into_the_editor_and_saves()
    {
        var editor = await EditorOnAsync("vetements_tisses_et_prets_a_porter");
        editor.SelectedGridKind = editor.GridKinds.Single(k => k.Code == "mensuel_cadres_maitrise");
        editor.TableNumber = "2";
        editor.StartDate = new DateTime(2024, 1, 1);
        _app.Dialogs.FileToOpen = Draft("rider-18_table-2.csv");

        await editor.ImportCsvCommand.ExecuteAsync(null);

        Assert.Empty(_app.Dialogs.Errors);
        Assert.Equal((18, 18), (editor.Categories.Count, editor.Degrees.Count));
        Assert.Equal(["1أ", "1ب", "2"], editor.Categories.Take(3).Select(c => c.Label));
        Assert.Equal([1, 1, 1, 2], editor.Degrees.Take(4).Select(d => d.YearsInDegree));
        Assert.Equal(595.401m, editor.Categories[0][editor.Degrees[0].Key]);
        Assert.Equal(1384.101m, editor.Categories[17][editor.Degrees[17].Key]);

        await editor.SaveCommand.ExecuteAsync(null);

        Assert.Empty(_app.Dialogs.Errors);
        var reopened = _app.Get<SalaryTableEditorViewModel>();
        await reopened.LoadAsync(editor.Id);
        Assert.Equal(324, reopened.Categories.Sum(c => reopened.Degrees.Count(d => c[d.Key] is not null)));
        Assert.Equal(SalaryType.Monthly, reopened.SalaryType);
    }

    [Fact]
    public async Task Importing_over_an_existing_grid_asks_first_and_a_bad_file_names_its_line()
    {
        var editor = await EditorOnAsync("textile");
        editor.AddCategoryCommand.Execute(null);
        var bad = Path.Combine(Path.GetTempPath(), $"grid-{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(bad, "category,1,2\nyears,1,1\nI,1,x\n");
        try
        {
            _app.Dialogs.FileToOpen = bad;
            _app.Dialogs.ConfirmAnswer = false;
            await editor.ImportCsvCommand.ExecuteAsync(null);
            Assert.Single(editor.Categories);
            Assert.Empty(_app.Dialogs.Errors);

            _app.Dialogs.ConfirmAnswer = true;
            await editor.ImportCsvCommand.ExecuteAsync(null);
            Assert.Contains("3", Assert.Single(_app.Dialogs.Errors), StringComparison.Ordinal);
            Assert.Single(editor.Categories); // nothing replaced when the file is refused
        }
        finally
        {
            File.Delete(bad);
        }
    }

    private async Task<AllowanceEditorViewModel> PresenceOfRider18Async()
    {
        var editor = _app.Get<AllowanceEditorViewModel>();
        await editor.LoadAsync(null);
        editor.SelectedAgreementId = editor.Agreements.Single(a => a.Code == "vetements_tisses_et_prets_a_porter").Id;
        editor.Name = "منحة الحضور";
        editor.Unit = PayrollCalculator.Domain.Allowances.AllowanceUnit.PerMonth;
        editor.AppliesTo = "كافة العمال";
        foreach (var (year, value) in new[] { (2024, 7.930m), (2025, 8.445m), (2026, 9.036m) })
        {
            editor.AddRateCommand.Execute(null);
            editor.Rates[^1].EffectiveFrom = new DateTime(year, 1, 1);
            editor.Rates[^1].Value = value;
            editor.Rates[^1].Source ??= "الرائد الرسمي عدد 49 لسنة 2024، ص 3516، الفصل 49";
        }

        return editor;
    }

    [Fact]
    public async Task A_prime_with_its_dated_amounts_is_saved_and_listed_with_the_amount_in_force()
    {
        var shell = _app.Get<MainViewModel>();
        var editor = await PresenceOfRider18Async();
        Assert.All(editor.Rates, r => Assert.StartsWith("الرائد الرسمي", r.Source, StringComparison.Ordinal)); // copied to each new line

        await editor.SaveCommand.ExecuteAsync(null);

        Assert.Empty(_app.Dialogs.Errors);
        var list = Assert.IsType<AllowancesViewModel>(shell.CurrentPage);
        var row = Assert.Single(list.Items);
        Assert.Equal(("منحة الحضور", 3, new DateOnly(2024, 1, 1)), (row.Name, row.RateCount, row.FirstDate));
        Assert.NotNull(row.CurrentValue);

        var reopened = _app.Get<AllowanceEditorViewModel>();
        await reopened.LoadAsync(row.Id);
        Assert.Equal([7.930m, 8.445m, 9.036m], reopened.Rates.Select(r => r.Value!.Value));
        Assert.Equal("كافة العمال", reopened.AppliesTo);
    }

    [Fact]
    public async Task A_prime_line_without_a_date_or_with_a_repeated_date_is_refused()
    {
        var editor = await PresenceOfRider18Async();
        editor.AddRateCommand.Execute(null);

        await editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(Labels.RateIncomplete, Assert.Single(_app.Dialogs.Errors));

        editor.Rates[^1].EffectiveFrom = new DateTime(2025, 1, 1);
        editor.Rates[^1].Value = 1m;
        await editor.SaveCommand.ExecuteAsync(null);
        Assert.Equal(2, _app.Dialogs.Errors.Count);
        Assert.IsNotType<AllowancesViewModel>(_app.Get<MainViewModel>().CurrentPage); // the two dates 2025 clash: still on the form
    }

    [Fact]
    public async Task A_prime_is_deleted_from_its_row_after_confirmation()
    {
        var editor = await PresenceOfRider18Async();
        await editor.SaveCommand.ExecuteAsync(null);
        var list = (AllowancesViewModel)_app.Get<MainViewModel>().CurrentPage!;

        await list.DeleteCommand.ExecuteAsync(list.Items[0]);

        Assert.Empty(list.Items);
    }

    private static string TravelDraft(string file, [System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", "data", "seed", "drafts", "agences_de_voyages", file));

    private async Task<int> TravelGridAsync(string file, DateTime start)
    {
        var grid = await EditorOnAsync("agences_de_voyages");
        grid.SelectedGridKind = grid.GridKinds[0];
        grid.TableNumber = "1";
        grid.StartDate = start;
        _app.Dialogs.FileToOpen = TravelDraft(file);
        await grid.ImportCsvCommand.ExecuteAsync(null);
        await grid.SaveCommand.ExecuteAsync(null);
        return grid.Id;
    }

    private async Task TravelPrimeAsync(string name, PayrollCalculator.Domain.Allowances.AllowanceUnit unit, string? appliesTo, params (int Year, decimal Value)[] rates)
    {
        var prime = _app.Get<AllowanceEditorViewModel>();
        await prime.LoadAsync(null);
        prime.SelectedAgreementId = prime.Agreements.Single(a => a.Code == "agences_de_voyages").Id;
        prime.Name = name;
        prime.Unit = unit;
        prime.AppliesTo = appliesTo;
        foreach (var (year, value) in rates)
        {
            prime.AddRateCommand.Execute(null);
            prime.Rates[^1].EffectiveFrom = new DateTime(year, 5, 1);
            prime.Rates[^1].Value = value;
            prime.Rates[^1].Source = "الرائد الرسمي عدد 120 لسنة 2022، ص 3265";
        }

        await prime.SaveCommand.ExecuteAsync(null);
    }

    /// <summary>Travel agencies (rider 11): two grids, transport (a group) and presence (everyone), one affair May 2022 to June 2023.</summary>
    private async Task<LedgerViewModel> TravelLedgerAsync()
    {
        var table2022 = await TravelGridAsync("rider-11_table-1.csv", new DateTime(2022, 5, 1));
        await TravelGridAsync("rider-11_table-2.csv", new DateTime(2023, 5, 1));
        await TravelPrimeAsync("منحة النقل", PayrollCalculator.Domain.Allowances.AllowanceUnit.PerMonth, "أعوان التنفيذ والتسيير", (2022, 75.089m), (2023, 79.970m));
        await TravelPrimeAsync("منحة الحضور", PayrollCalculator.Domain.Allowances.AllowanceUnit.PerDayWorked, null, (2022, 0.452m), (2023, 0.481m));
        Assert.Empty(_app.Dialogs.Errors);

        var affair = _app.Get<AffairEditorViewModel>();
        await affair.LoadAsync(null);
        affair.AffairNumber = "2026-7";
        affair.Claimant = "Salem";
        affair.StartDate = new DateTime(2022, 5, 1);
        affair.EndDate = new DateTime(2023, 6, 30);
        affair.SelectedAgreementId = affair.Agreements.Single(a => a.Code == "agences_de_voyages").Id;
        await affair.PendingLoad;
        affair.SelectedTableId = table2022;
        await affair.PendingLoad;
        affair.InitialCategory = "1";
        affair.InitialDegree = 1;
        await affair.SaveCommand.ExecuteAsync(null);
        Assert.Empty(_app.Dialogs.Errors);

        var ledger = _app.Get<LedgerViewModel>();
        await ledger.LoadAsync(affair.Id);
        return ledger;
    }

    /// <summary>Arrears: owed minus paid, month by month, overpaid months kept negative.</summary>
    [Fact]
    public async Task Paid_steps_give_the_difference_owed_minus_paid_for_each_month()
    {
        var ledger = await TravelLedgerAsync();
        Assert.All(ledger.Rows, r => Assert.Null(r.Difference)); // nothing paid entered yet
        Assert.Equal(14, ledger.MonthsWithoutPaid);

        ledger.AddPaidStepCommand.Execute(null);
        ledger.PaidSteps[^1].From = new DateTime(2022, 5, 1);
        ledger.PaidSteps[^1].GrossAmount = 600m;
        ledger.PaidSteps[^1].Note = "بطاقة خلاص ماي 2022";
        ledger.AddPaidStepCommand.Execute(null);
        ledger.PaidSteps[^1].From = new DateTime(2023, 1, 1);
        ledger.PaidSteps[^1].GrossAmount = 500m;
        await ledger.SavePaidCommand.ExecuteAsync(null);

        Assert.Empty(_app.Dialogs.Errors);
        var may2022 = ledger.Rows[0];
        Assert.Equal((600m, 504.153m + 11.752m - 600m), (may2022.Paid, may2022.Difference)); // paid more than owed: negative, kept
        var jan2023 = ledger.Rows[8];
        Assert.Equal(("2023-01", 500m, 504.153m + 11.752m - 500m), (jan2023.Month, jan2023.Paid, jan2023.Difference));
        Assert.Equal(8 * 600m + 6 * 500m, ledger.TotalPaid);
        Assert.Equal(ledger.GrandTotal - ledger.TotalPaid, ledger.TotalDifference);
        Assert.Equal(0, ledger.MonthsWithoutPaid);

        var reopened = _app.Get<LedgerViewModel>();
        await reopened.LoadAsync(ledger.Affair!.Id);
        Assert.Equal([600m, 500m], reopened.PaidSteps.Select(p => p.GrossAmount!.Value));
        Assert.Equal(ledger.TotalDifference, reopened.TotalDifference);
    }

    [Fact]
    public async Task A_paid_step_without_an_amount_is_refused()
    {
        var ledger = await TravelLedgerAsync();
        ledger.AddPaidStepCommand.Execute(null);
        ledger.PaidSteps[^1].From = new DateTime(2022, 5, 1);

        await ledger.SavePaidCommand.ExecuteAsync(null);

        Assert.Equal(Labels.RateIncomplete, Assert.Single(_app.Dialogs.Errors));
    }

    /// <summary>Task 3, end to end: real grids and primes of the travel agencies (rider 11), one employee, May 2022 to June 2023.</summary>
    [Fact]
    public async Task The_monthly_ledger_combines_the_grids_and_the_chosen_primes()
    {
        var ledger = await TravelLedgerAsync();

        // The prime for everyone is ticked; the one for a group is left to the expert.
        var transport = ledger.Primes.Single(p => p.Name == "منحة النقل");
        Assert.False(transport.IsSelected);
        Assert.True(ledger.Primes.Single(p => p.Name == "منحة الحضور").IsSelected);
        Assert.Equal(2, ledger.Columns.Count);

        transport.IsSelected = true;
        await ledger.CalculateCommand.ExecuteAsync(null);

        Assert.Empty(_app.Dialogs.Errors);
        Assert.Equal(3, ledger.Columns.Count);
        Assert.Equal(14, ledger.Rows.Count);
        var may2022 = ledger.Rows[0];
        var may2023 = ledger.Rows[12];
        Assert.Equal(("2022-05", "1", 1, 504.153m), (may2022.Month, may2022.Category, may2022.Degree, may2022[ledger.Columns[0].Code]));
        Assert.Equal(504.153m + 75.089m + 11.752m, may2022.Total);
        Assert.Equal((2, 550.345m), (may2023.Degree, may2023[ledger.Columns[0].Code])); // new grid and degree 2 from 1 May 2023
        Assert.Equal(550.345m + 79.970m + 12.506m, may2023.Total);
        Assert.Equal(0, ledger.UnresolvedMonths);
        Assert.Equal(ledger.Rows.Sum(r => r.Total), ledger.GrandTotal);

        var csv = Path.Combine(Path.GetTempPath(), $"ledger-{Guid.NewGuid():N}.csv");
        try
        {
            _app.Dialogs.FileToSave = csv;
            await ledger.ExportCsvCommand.ExecuteAsync(null);
            var lines = await File.ReadAllLinesAsync(csv);
            Assert.Contains("منحة النقل", lines[0], StringComparison.Ordinal);
            Assert.StartsWith("2022-05;1;1;", lines[1], StringComparison.Ordinal);
            Assert.StartsWith("Total;", lines[^1], StringComparison.Ordinal);
            Assert.Equal(16, lines.Length); // header, 14 months, totals
        }
        finally
        {
            File.Delete(csv);
        }
    }

    [Fact]
    public async Task The_grid_kind_sets_the_weekly_hours_and_a_saved_table_keeps_them()
    {
        var editor = await EditorOnAsync("banques_et_institutions_financieres");
        Assert.Equal(48, editor.WeeklyHours);

        editor.SelectedGridKind = editor.GridKinds[0];
        Assert.Equal(40, editor.WeeklyHours);
        editor.TableNumber = "1";
        editor.StartDate = new DateTime(2024, 1, 1);
        await editor.SaveCommand.ExecuteAsync(null);
        Assert.Empty(_app.Dialogs.Errors);

        var reopened = _app.Get<SalaryTableEditorViewModel>();
        await reopened.LoadAsync(editor.Id);
        Assert.Equal(40, reopened.WeeklyHours);
    }

    [Fact]
    public async Task Domain_errors_are_shown_not_thrown()
    {
        var vm = _app.Get<AgreementsViewModel>();
        await vm.LoadAsync();
        vm.NewCommand.Execute(null);
        vm.EditName = " ";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Single(_app.Dialogs.Errors);
        Assert.Empty(UserAgreements(vm));
    }

    [Fact]
    public async Task Salary_table_editor_round_trips_the_grid()
    {
        var saved = await SavedTableAsync();
        Assert.True(saved.Id > 0);
        Assert.Empty(_app.Dialogs.Errors);

        var reopened = _app.Get<SalaryTableEditorViewModel>();
        await reopened.LoadAsync(saved.Id);

        Assert.Equal("4", reopened.TableNumber);
        Assert.Equal([1, 2], reopened.Degrees.Select(d => d.Number));
        Assert.Equal(350m, reopened.Categories[0][reopened.Degrees[1].Key]);
    }

    [Fact]
    public async Task New_degrees_are_numbered_after_the_last_one()
    {
        var editor = _app.Get<SalaryTableEditorViewModel>();
        await editor.LoadAsync(null);

        editor.AddDegreeCommand.Execute(null);
        editor.AddDegreeCommand.Execute(null);

        Assert.Equal([1, 2], editor.Degrees.Select(d => d.Number));
    }

    [Fact]
    public async Task Removing_a_degree_drops_its_amounts()
    {
        var editor = _app.Get<SalaryTableEditorViewModel>();
        await editor.LoadAsync(null);
        editor.AddCategoryCommand.Execute(null);
        editor.AddDegreeCommand.Execute(null);
        var key = editor.Degrees[0].Key;
        editor.Categories[0][key] = 5m;

        editor.RemoveDegreeCommand.Execute(editor.Degrees[0]);

        Assert.Empty(editor.Degrees);
        Assert.Null(editor.Categories[0][key]);
    }

    [Fact]
    public async Task Affair_editor_offers_the_tables_and_categories_of_the_selection()
    {
        var table = await SavedTableAsync();
        var editor = _app.Get<AffairEditorViewModel>();
        await editor.LoadAsync(null);

        editor.SelectedAgreementId = table.SelectedAgreementId;
        await editor.PendingLoad;
        Assert.Equal("4", Assert.Single(editor.Tables).TableNumber);

        editor.SelectedTableId = table.Id;
        await editor.PendingLoad;
        Assert.Equal(["C1"], editor.CategoryOptions);
        Assert.Equal([1, 2], editor.DegreeOptions);
    }

    [Fact]
    public async Task Timeline_shows_monthly_lines_and_exports_csv()
    {
        var table = await SavedTableAsync();
        var editor = _app.Get<AffairEditorViewModel>();
        await editor.LoadAsync(null);
        editor.AffairNumber = "2024-1";
        editor.Claimant = "Salem";
        editor.StartDate = new DateTime(2020, 1, 1);
        editor.EndDate = new DateTime(2021, 12, 31);
        editor.SelectedAgreementId = table.SelectedAgreementId;
        await editor.PendingLoad;
        editor.SelectedTableId = table.Id;
        await editor.PendingLoad;
        editor.InitialCategory = "C1";
        editor.InitialDegree = 1;
        await editor.SaveCommand.ExecuteAsync(null);
        Assert.Empty(_app.Dialogs.Errors);

        var timeline = _app.Get<TimelineViewModel>();
        await timeline.LoadAsync(editor.Id);

        Assert.Equal(24, timeline.Lines.Count);
        Assert.Equal(12 * 300m + 12 * 350m, timeline.TotalMonthlyAmount);
        Assert.Equal(0, timeline.UnresolvedMonths);
        Assert.All(timeline.Lines, l => Assert.Equal(TimelineIssue.None, l.Issue));

        var csv = Path.Combine(Path.GetTempPath(), $"timeline-{Guid.NewGuid():N}.csv");
        _app.Dialogs.FileToSave = csv;
        await timeline.ExportCsvCommand.ExecuteAsync(null);
        Assert.Equal(25, File.ReadAllLines(csv).Length);
        File.Delete(csv);
    }

    [Fact]
    public async Task Navigation_opens_the_editor_from_the_list()
    {
        await AgreementsWithOneAsync();
        var navigator = _app.Get<INavigator>();
        var list = _app.Get<SalaryTablesViewModel>();
        await list.LoadAsync();

        await list.NewCommand.ExecuteAsync(null);

        Assert.IsType<SalaryTableEditorViewModel>(navigator.Current);
    }
}
