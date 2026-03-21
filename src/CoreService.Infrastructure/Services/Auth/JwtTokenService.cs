using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Abstractions.Common;
using CoreService.Domain.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CoreService.Infrastructure.Services.Auth;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _opt;
    private readonly IClock _clock;

    public JwtTokenService(IOptions<JwtOptions> opt, IClock clock)
    {
        _opt = opt.Value;
        _clock = clock;
    }

    public Task<AccessTokenResult> CreateAccessTokenAsync(
        Guid userId,
        string roleCode,
        string email,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opt.SigningKey))
            throw new InvalidOperationException("Jwt:SigningKey is missing");

        var now = _clock.UtcNow;
        var expires = now.AddMinutes(_opt.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtClaimNames.Subject, userId.ToString()),
            new(JwtClaimNames.Role, roleCode),
            new(JwtClaimNames.Email, email),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: string.IsNullOrWhiteSpace(_opt.Issuer) ? null : _opt.Issuer,
            audience: string.IsNullOrWhiteSpace(_opt.Audience) ? null : _opt.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return Task.FromResult(new AccessTokenResult(jwt, expires));
    }
}
