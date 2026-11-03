using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace PayrollCalculator.Wpf.Services;

/// <summary>User dialogs, abstracted so view models stay testable.</summary>
public interface IDialogService
{
    bool Confirm(string message);

    void ShowError(string message);

    void ShowInfo(string message);

    /// <returns>The chosen path, or null when cancelled.</returns>
    string? OpenFile(string filter);

    /// <returns>The chosen path, or null when cancelled.</returns>
    string? SaveFile(string filter, string defaultFileName);
}

/// <summary>Runs one use case in its own DI scope, i.e. with a fresh DbContext (unit of work).</summary>
public interface IServiceRunner
{
    Task<TResult> RunAsync<TService, TResult>(Func<TService, Task<TResult>> action) where TService : notnull;

    Task RunAsync<TService>(Func<TService, Task> action) where TService : notnull;
}

public sealed class ServiceRunner(IServiceScopeFactory scopes) : IServiceRunner
{
    public async Task<TResult> RunAsync<TService, TResult>(Func<TService, Task<TResult>> action) where TService : notnull
    {
        ArgumentNullException.ThrowIfNull(action);
        await using var scope = scopes.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<TService>()).ConfigureAwait(true);
    }

    public async Task RunAsync<TService>(Func<TService, Task> action) where TService : notnull
    {
        ArgumentNullException.ThrowIfNull(action);
        await using var scope = scopes.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<TService>()).ConfigureAwait(true);
    }
}

/// <summary>Page navigation inside the main window.</summary>
public interface INavigator
{
    object? Current { get; }

    /// <summary>Creates the page view model, lets <paramref name="initialize"/> load it, then shows it.</summary>
    Task GoAsync<TPage>(Func<TPage, Task>? initialize = null) where TPage : notnull;
}

public sealed partial class Navigator(IServiceProvider services) : ObservableObject, INavigator
{
    [ObservableProperty]
    public partial object? Current { get; private set; }

    public async Task GoAsync<TPage>(Func<TPage, Task>? initialize = null) where TPage : notnull
    {
        var page = services.GetRequiredService<TPage>();
        if (initialize is not null)
        {
            await initialize(page).ConfigureAwait(true);
        }

        Current = page;
    }
}
