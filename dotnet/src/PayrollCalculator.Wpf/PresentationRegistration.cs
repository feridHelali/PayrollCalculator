using Microsoft.Extensions.DependencyInjection;
using PayrollCalculator.Wpf.Services;
using PayrollCalculator.Wpf.ViewModels;

namespace PayrollCalculator.Wpf;

public static class PresentationRegistration
{
    /// <summary>Registers view models and UI services (views are resolved by WPF data templates).</summary>
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddSingleton<IServiceRunner, ServiceRunner>();
        services.AddSingleton<IDialogService, WpfDialogService>();
        services.AddSingleton<StatusViewModel>();
        services.AddSingleton<Navigator>();
        services.AddSingleton<INavigator>(sp => sp.GetRequiredService<Navigator>());
        services.AddSingleton<MainViewModel>();

        services.AddTransient<AgreementsViewModel>();
        services.AddTransient<SalaryTablesViewModel>();
        services.AddTransient<SalaryTableEditorViewModel>();
        services.AddTransient<AffairsViewModel>();
        services.AddTransient<AffairEditorViewModel>();
        services.AddTransient<TimelineViewModel>();
        return services;
    }
}
