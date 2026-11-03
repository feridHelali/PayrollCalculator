using Microsoft.Extensions.DependencyInjection;
using PayrollCalculator.Application.Affairs;
using PayrollCalculator.Application.Agreements;
using PayrollCalculator.Application.SalaryTables;
using PayrollCalculator.Domain.Calculation;

namespace PayrollCalculator.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IDegreeProgressionPolicy, SeniorityDegreeProgression>();
        services.AddSingleton<SalaryTimelineCalculator>();
        services.AddScoped<AgreementService>();
        services.AddScoped<SalaryTableService>();
        services.AddScoped<AffairService>();
        return services;
    }
}
