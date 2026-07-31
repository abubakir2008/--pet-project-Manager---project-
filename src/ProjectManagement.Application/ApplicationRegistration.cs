using Microsoft.Extensions.DependencyInjection;
using ProjectManagement.Application.Abstractions;
using ProjectManagement.Application.Services;

namespace ProjectManagement.Application;

/// <summary>Composition root of the logic layer.</summary>
public static class ApplicationRegistration
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddScoped<IAccessControlService, AccessControlService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IWorkTaskService, WorkTaskService>();
        services.AddScoped<ITokenService, TokenService>();

        return services;
    }
}
