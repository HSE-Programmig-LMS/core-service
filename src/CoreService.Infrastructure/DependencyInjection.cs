using CoreService.Application.Abstractions.Audit;
using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Abstractions.Common;
using CoreService.Application.Abstractions.Users;
using CoreService.Infrastructure.Identity;
using CoreService.Infrastructure.Persistence;
using CoreService.Infrastructure.Services.Audit;
using CoreService.Infrastructure.Services.Auth;
using CoreService.Infrastructure.Services.Common;
using CoreService.Infrastructure.Services.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<CoreDbContext>(opt =>
        {
            var cs = cfg.GetConnectionString("Db")
                     ?? throw new InvalidOperationException("ConnectionStrings:Db is missing");
            opt.UseNpgsql(cs);
        });

        services
            .AddIdentityCore<ApplicationUser>(opt =>
            {
                opt.User.RequireUniqueEmail = true;

                opt.Password.RequiredLength = 8;
                opt.Password.RequireNonAlphanumeric = false;
                opt.Password.RequireUppercase = true;
                opt.Password.RequireLowercase = true;
                opt.Password.RequireDigit = true;

                opt.Lockout.MaxFailedAccessAttempts = 10;
                opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<CoreDbContext>();

        // Options for JWT
        services.AddOptions<JwtOptions>()
            .Bind(cfg.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.SigningKey), "Jwt:SigningKey is required")
            .Validate(o => o.AccessTokenLifetimeMinutes > 0, "Jwt:AccessTokenLifetimeMinutes must be > 0");

        // Common
        services.AddSingleton<IClock, SystemClock>();

        // Auth
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordVerifier, PasswordVerifier>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();

        // Users
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();

        // Audit
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IAuditIngestStore, AuditIngestStore>();

        return services;
    }
}