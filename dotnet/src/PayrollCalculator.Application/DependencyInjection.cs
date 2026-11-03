using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PayrollCalculator.Application.Affairs;
using PayrollCalculator.Application.Agreements;
using PayrollCalculator.Application.SalaryTables;
using PayrollCalculator.Application.Users;
using PayrollCalculator.Domain.Calculation;
using PayrollCalculator.Domain.Users;

namespace PayrollCalculator.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IDegreeProgressionPolicy, SeniorityDegreeProgression>();
        services.AddSingleton<SalaryTimelineCalculator>();
        services.AddScoped<AgreementService>();
        services.AddScoped<SalaryTableService>();
        services.AddScoped<AffairService>();
        services.AddScoped<LedgerService>();
        services.AddScoped<Allowances.AllowanceService>();

        services.AddSingleton(PasswordPolicy.Default);
        services.AddSingleton<CurrentUser>();
        services.AddScoped<AuthenticationService>();
        services.AddScoped<UserService>();
        services.AddScoped<UserSeeder>();
        return services;
    }
}
