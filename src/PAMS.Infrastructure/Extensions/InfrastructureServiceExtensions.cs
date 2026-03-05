using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PAMS.Application.Interfaces;
using PAMS.Domain.Common;
using PAMS.Domain.Repositories;
using PAMS.Infrastructure.Persistence;
using PAMS.Infrastructure.Persistence.Repositories;
using PAMS.Infrastructure.Services;

namespace PAMS.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        // DbContext
        services.AddDbContext<PamsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "pams");
            }));

        // UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAllocationRepository, AllocationRepository>();
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();
        services.AddScoped<IProjectTeamMemberRepository, ProjectTeamMemberRepository>();

        // Services
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IExportService, ExportService>();

        return services;
    }
}
