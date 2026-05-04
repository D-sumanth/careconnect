using CareConnect.Application.Abstractions;
using CareConnect.Domain.Constants;
using CareConnect.Infrastructure.Identity;
using CareConnect.Infrastructure.Persistence;
using CareConnect.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CareConnect.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCareConnectInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options =>
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
            }
            else
            {
                options.UseInMemoryDatabase("CareConnectDevelopment");
            }
        });

        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.SignIn.RequireConfirmedEmail = false;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "__Host-CareConnect";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.SlidingExpiration = true;
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(CareConnectRoles.Admin));
            options.AddPolicy("DepartmentLeadOnly", policy => policy.RequireRole(CareConnectRoles.DepartmentLead));
        });

        services.AddScoped<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IAcknowledgementService, AcknowledgementService>();
        services.AddScoped<IAdminReportService, AdminReportService>();
        services.AddScoped<ISeedDataService, SeedDataService>();

        return services;
    }
}
