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
