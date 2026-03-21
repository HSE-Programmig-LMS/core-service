using CoreService.Application.Abstractions.Audit;
using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Abstractions.Common;
using CoreService.Application.Abstractions.Users;
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

        // Options for JWT (для JwtTokenService)
        services.AddOptions<JwtOptions>()
            .Bind(cfg.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.SigningKey), "Jwt:SigningKey is required")
            .Validate(o => o.AccessTokenLifetimeMinutes > 0, "Jwt:AccessTokenLifetimeMinutes must be > 0");

        // Common
        services.AddSingleton<IClock, SystemClock>();

        // Auth implementations
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordVerifier, PasswordVerifier>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();

        // Users
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();

        // Audit
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IAuditIngestStore, AuditIngestStore>();

        return services;
    }
}