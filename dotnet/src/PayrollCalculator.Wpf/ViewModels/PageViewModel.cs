using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using PayrollCalculator.Application.Common;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Wpf.Resources;
using PayrollCalculator.Wpf.Services;

namespace PayrollCalculator.Wpf.ViewModels;

/// <summary>Shared status bar state.</summary>
public sealed partial class StatusViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Message { get; set; } = Labels.Ready;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }
}

/// <summary>Base for pages: access to use cases, dialogs and a uniform way to run and report operations.</summary>
public abstract class PageViewModel(IServiceRunner runner, IDialogService dialogs, StatusViewModel status) : ObservableObject
{
    public abstract string Title { get; }

    protected IServiceRunner Runner { get; } = runner;

    protected IDialogService Dialogs { get; } = dialogs;

    protected StatusViewModel Status { get; } = status;

    /// <summary>Runs <paramref name="action"/>; expected business/IO errors are shown to the user instead of crashing.</summary>
    /// <returns>True when the action succeeded.</returns>
    protected async Task<bool> TryAsync(Func<Task> action, string? successMessage = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        Status.IsBusy = true;
        Status.Message = Labels.Loading;
        try
        {
            await action().ConfigureAwait(true);
            Status.Message = successMessage ?? Labels.Ready;
            return true;
        }
        catch (Exception ex) when (ex is DomainException or NotFoundException or InvalidOperationException
                                       or DbUpdateException or IOException or UnauthorizedAccessException)
        {
            var message = ex is DbUpdateException { InnerException: { } inner } ? inner.Message : ex.Message;
            Status.Message = $"{Labels.Error}: {message}";
            Dialogs.ShowError(message);
            return false;
        }
        finally
        {
            Status.IsBusy = false;
        }
    }

    protected static DateOnly? ToDate(DateTime? value) => value is { } v ? DateOnly.FromDateTime(v) : null;

    protected static DateTime? ToDateTime(DateOnly? value) => value?.ToDateTime(TimeOnly.MinValue);
}
