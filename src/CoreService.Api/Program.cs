using System.Text;
using CoreService.Api.Auth;
using CoreService.Api.Common.Authorization;
using CoreService.Application.Abstractions.Auth;
using CoreService.Application.UseCases.Auth;
using CoreService.Application.UseCases.Users;
using CoreService.Infrastructure;
using CoreService.Infrastructure.Identity;
using CoreService.Infrastructure.Persistence;
using CoreService.Infrastructure.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger/OpenAPI (dev)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "CoreService API", Version = "v1" });

    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });
});

// Infra: DbContext + реализации интерфейсов (JwtTokenService, repos, audit, etc.)
builder.Services.AddCoreInfrastructure(builder.Configuration);

// Http context access + IUserContext
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, HttpUserContext>();

// Identity wiring
builder.Services
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
    .AddEntityFrameworkStores<CoreDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// JWT auth
var signingKey = builder.Configuration["Jwt:SigningKey"]
                 ?? throw new InvalidOperationException("Jwt:SigningKey missing");

var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),

            ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
            ValidIssuer = issuer,

            ValidateAudience = !string.IsNullOrWhiteSpace(audience),
            ValidAudience = audience,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

// Authorization policies (RequireManager)
builder.Services.AddAuthorization(o => Policies.AddAuthorizationPolicies(o));

// Health checks
builder.Services.AddHealthChecks();

// UseCases registration
// Refresh lifetime from config (days). Default 30.
var refreshDays = builder.Configuration.GetValue<int?>("Auth:RefreshTokenLifetimeDays") ?? 30;
var refreshLifetime = TimeSpan.FromDays(refreshDays);

// Auth use-cases
builder.Services.AddScoped(sp =>
    new LoginUseCase(
        passwordVerifier: sp.GetRequiredService<CoreService.Application.Abstractions.Auth.IPasswordVerifier>(),
        jwtTokenService: sp.GetRequiredService<CoreService.Application.Abstractions.Auth.IJwtTokenService>(),
        refreshTokenStore: sp.GetRequiredService<CoreService.Application.Abstractions.Auth.IRefreshTokenStore>(),
        clock: sp.GetRequiredService<CoreService.Application.Abstractions.Common.IClock>(),
        refreshTokenLifetime: refreshLifetime));

builder.Services.AddScoped(sp =>
    new RefreshUseCase(
        refreshTokenStore: sp.GetRequiredService<CoreService.Application.Abstractions.Auth.IRefreshTokenStore>(),
        users: sp.GetRequiredService<CoreService.Application.Abstractions.Users.IUserRepository>(),
        jwtTokenService: sp.GetRequiredService<CoreService.Application.Abstractions.Auth.IJwtTokenService>(),
        clock: sp.GetRequiredService<CoreService.Application.Abstractions.Common.IClock>(),
        refreshTokenLifetime: refreshLifetime));

builder.Services.AddScoped<LogoutUseCase>();
builder.Services.AddScoped<GetMeUseCase>();
builder.Services.AddScoped<ForgotPasswordUseCase>();
builder.Services.AddScoped<ResetPasswordUseCase>();

// Users use-cases
builder.Services.AddScoped<CreateUserUseCase>();
builder.Services.AddScoped<UpdateUserUseCase>();
builder.Services.AddScoped<DeactivateUserUseCase>();
builder.Services.AddScoped<ChangeUserRoleUseCase>();
builder.Services.AddScoped<GetUsersUseCase>();
builder.Services.AddScoped<GetUserByIdUseCase>();

// Build app
var app = builder.Build();

// Map health
app.MapHealthChecks("/health");

// Dev bootstrap: migrate + seed roles
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
    await db.Database.MigrateAsync();

    var seeder = new RoleSeeder(scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>());
    await seeder.SeedAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();