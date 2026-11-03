using PayrollCalculator.Infrastructure;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;
using PayrollCalculator.Wpf.ViewModels;

namespace PayrollCalculator.Wpf.Tests;

public sealed class AboutTests : IDisposable
{
    private readonly TestApp _app = new();

    public void Dispose() => _app.Dispose();

    [Fact]
    public void Product_version_is_1_0_0()
    {
        var product = ProductInfo.Current;

        Assert.Equal("1.0.0", product.Version);
        Assert.Equal(Labels.AppTitle, product.Name);
    }

    [Fact]
    public void About_shows_version_idea_developer_and_database()
    {
        var about = _app.Get<AboutViewModel>();

        Assert.Equal(Labels.About, about.Title);
        Assert.Equal("1.0.0", about.Product.Version);
        Assert.Equal("Lazhar Lataoui", Labels.IdeaBy);
        Assert.Equal(_app.Get<DatabaseOptions>().DatabasePath, about.DatabasePath);
        Assert.Contains(about.Runtime, about.SupportText, StringComparison.Ordinal);
    }

    [Fact]
    public void Support_text_is_a_pasteable_block_without_user_data()
    {
        var text = _app.Get<AboutViewModel>().SupportText;

        Assert.Contains("1.0.0", text, StringComparison.Ordinal);
        Assert.Contains("Lazhar Lataoui", text, StringComparison.Ordinal);
        Assert.Contains("Ferid HELALI", text, StringComparison.Ordinal);
        Assert.DoesNotContain("admin", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task About_has_its_own_menu_entry()
    {
        var shell = _app.Get<MainViewModel>();

        await shell.ShowAboutCommand.ExecuteAsync(null);

        Assert.Equal(NavSection.About, shell.ActiveSection);
        Assert.True(shell.IsAboutActive);
    }

    [Fact]
    public void Every_startup_stage_has_an_arabic_label_and_progress_grows()
    {
        var stages = Enum.GetValues<StartupStage>();

        Assert.All(stages, s => Assert.False(string.IsNullOrWhiteSpace(Labels.StartupStageText(s))));
        Assert.Equal(stages.OrderBy(s => (int)s), stages.OrderBy(StartupProgress.Of));
        Assert.Equal(1.0, StartupProgress.Of(StartupStage.Ready));
    }
}
