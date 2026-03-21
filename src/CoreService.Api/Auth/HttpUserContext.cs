using System.Security.Claims;
using CoreService.Application.Abstractions.Auth;
using CoreService.Domain.Auth;
using Microsoft.AspNetCore.Http;

namespace CoreService.Api.Auth;

public sealed class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _http;

    public HttpUserContext(IHttpContextAccessor http)
    {
        _http = http;
    }

    public bool IsAuthenticated =>
        _http.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var value = FindClaim(JwtClaimNames.Subject);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? RoleCode => FindClaim(JwtClaimNames.Role);

    public string? Email => FindClaim(JwtClaimNames.Email);

    private string? FindClaim(string type)
    {
        var user = _http.HttpContext?.User;
        if (user is null) return null;

        // сначала ищем по точному типу
        var v = user.FindFirstValue(type);
        if (!string.IsNullOrWhiteSpace(v)) return v;

        // fallback: иногда subject может приходить как ClaimTypes.NameIdentifier
        if (type == JwtClaimNames.Subject)
        {
            v = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(v)) return v;
        }

        // fallback: иногда email приходит как ClaimTypes.Email
        if (type == JwtClaimNames.Email)
        {
            v = user.FindFirstValue(ClaimTypes.Email);
            if (!string.IsNullOrWhiteSpace(v)) return v;
        }

        return null;
    }
}