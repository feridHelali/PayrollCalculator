using System.Reflection;
using PayrollCalculator.Wpf.Resources;

namespace PayrollCalculator.Wpf.Services;

/// <summary>Name, version and build of the running application, read from the assembly attributes.</summary>
/// <remarks>
/// The version is set once, in <c>Directory.Build.props</c>. The SDK appends the git commit to the informational
/// version (<c>1.0.0+abc1234…</c>); it is shown separately so support knows exactly which build is installed.
/// </remarks>
public sealed record ProductInfo(string Name, string Version, string? Commit, string Copyright)
{
    public static ProductInfo Current { get; } = From(typeof(ProductInfo).Assembly);

    /// <summary>"1.0.0 (abc1234)" or "1.0.0".</summary>
    public string DisplayVersion => Commit is null ? Version : $"{Version} ({Commit})";

    public static ProductInfo From(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                            ?? assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        var parts = informational.Split('+', 2);
        var commit = parts.Length == 2 && parts[1].Length > 0 ? parts[1][..Math.Min(7, parts[1].Length)] : null;
        var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty;
        return new ProductInfo(Labels.AppTitle, parts[0], commit, copyright);
    }
}

/// <summary>What start-up is doing, in order. Shown on the splash screen.</summary>
public enum StartupStage
{
    Starting,
    Database,
    Accounts,
    LegacyImport,
    Ready,
}

public static class StartupProgress
{
    /// <summary>Progress bar value (0..1) once <paramref name="stage"/> has started.</summary>
    public static double Of(StartupStage stage) => stage switch
    {
        StartupStage.Starting => 0.1,
        StartupStage.Database => 0.35,
        StartupStage.Accounts => 0.65,
        StartupStage.LegacyImport => 0.85,
        _ => 1.0,
    };
}
