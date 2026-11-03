using System.Diagnostics;
using System.Windows;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;

namespace PayrollCalculator.Wpf.Views;

/// <summary>Shown first, while the database is prepared. The bar follows the real start-up stages.</summary>
/// <remarks>
/// A bar running at a fixed speed would sit at 100 % while a slow migration or import is still running, which is
/// exactly when the app most needs to look alive.
/// </remarks>
public partial class SplashWindow : Window, IProgress<StartupStage>
{
    /// <summary>Below this the splash would only flash; above it, it never delays a slow start.</summary>
    private static readonly TimeSpan MinimumDisplay = TimeSpan.FromMilliseconds(1500);

    /// <summary>Started when the splash is first drawn, not when it is created: process start-up must not count.</summary>
    private readonly Stopwatch _shown = new();

    public SplashWindow()
    {
        InitializeComponent();
        ContentRendered += (_, _) => _shown.Start();
        ProgressTrack.SizeChanged += (_, _) => UpdateBar();
        VersionText.Text = $"{Labels.Version} {ProductInfo.Current.DisplayVersion}";
        Report(StartupStage.Starting);
    }

    public void Report(StartupStage value)
    {
        StageText.Text = Labels.StartupStageText(value);
        _progress = StartupProgress.Of(value);
        UpdateBar();
    }

    private double _progress;

    /// <summary>A plain width, not an animation: start-up work runs on this thread, so an animation would stall anyway.</summary>
    private void UpdateBar() => ProgressFill.Width = ProgressTrack.ActualWidth * _progress;

    /// <summary>Shows "ready", keeps the splash up for its minimum time, then closes it.</summary>
    public async Task CompleteAsync()
    {
        Report(StartupStage.Ready);
        var remaining = MinimumDisplay - _shown.Elapsed;
        if (remaining > TimeSpan.Zero)
        {
            await Task.Delay(remaining).ConfigureAwait(true);
        }

        Close();
    }
}
