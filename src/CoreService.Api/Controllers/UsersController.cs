using CoreService.Api.Common;
using CoreService.Api.Common.Authorization;
using CoreService.Application.Contracts.Users;
using CoreService.Application.UseCases.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreService.Api.Controllers;

[ApiController]
[Route("users")]
[Authorize(Policy = Policies.RequireManager)]
public sealed class UsersController : ControllerBase
{
    public const string GetUserByIdRouteName = "GetUserById";

    private readonly CreateUserUseCase _create;
    private readonly UpdateUserUseCase _update;
    private readonly DeactivateUserUseCase _deactivate;
    private readonly ChangeUserRoleUseCase _changeRole;
    private readonly GetUsersUseCase _getUsers;
    private readonly GetUserByIdUseCase _getUserById;

    public UsersController(
        CreateUserUseCase create,
        UpdateUserUseCase update,
        DeactivateUserUseCase deactivate,
        ChangeUserRoleUseCase changeRole,
        GetUsersUseCase getUsers,
        GetUserByIdUseCase getUserById)
    {
        _create = create;
        _update = update;
        _deactivate = deactivate;
        _changeRole = changeRole;
        _getUsers = getUsers;
        _getUserById = getUserById;
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? emailContains,
        [FromQuery] string? role,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new UsersQuery(
            EmailContains: emailContains,
            Role: role,
            IsActive: isActive,
            Page: page,
            PageSize: pageSize);

        var result = await _getUsers.ExecuteAsync(query, ct);
        return result.ToActionResult(this);
    }

    [HttpGet("{id:guid}", Name = GetUserByIdRouteName)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _getUserById.ExecuteAsync(id, ct);
        return result.ToActionResult(this);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var result = await _create.ExecuteAsync(request, ct);
        return result.ToCreatedResult(
            controller: this,
            routeName: GetUserByIdRouteName,
            routeValues: new { id = result.Value?.UserId ?? Guid.Empty });
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var result = await _update.ExecuteAsync(id, request, ct);
        return result.ToActionResult(this);
    }

    [HttpPatch("{id:guid}/role")]
    public async Task<IActionResult> ChangeRole([FromRoute] Guid id, [FromBody] ChangeUserRoleRequest request, CancellationToken ct)
    {
        var result = await _changeRole.ExecuteAsync(id, request, ct);
        return result.ToActionResult(this);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate([FromRoute] Guid id, CancellationToken ct)
    {
        var result = await _deactivate.ExecuteAsync(id, ct);
        return result.ToActionResult(this);
    }
}