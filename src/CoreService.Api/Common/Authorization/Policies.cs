using CoreService.Domain.Security;
using Microsoft.AspNetCore.Authorization;

namespace CoreService.Api.Common.Authorization;

public static class Policies
{
    public const string RequireManager = "RequireManager";

    public static void AddAuthorizationPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(RequireManager, p => p.RequireRole(RoleCodeMapper.ToDb(RoleCode.Manager)));
    }
}