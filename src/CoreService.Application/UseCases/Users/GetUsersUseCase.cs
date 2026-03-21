using CoreService.Application.Abstractions.Users;
using CoreService.Application.Common.Errors;
using CoreService.Application.Common.Results;
using CoreService.Application.Contracts.Users;

namespace CoreService.Application.UseCases.Users;

public sealed class GetUsersUseCase
{
    private readonly IUserRepository _users;

    public GetUsersUseCase(IUserRepository users) => _users = users;

    public async Task<Result<PagedResult<UserDto>>> ExecuteAsync(UsersQuery query, CancellationToken ct = default)
    {
        if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 200)
        {
            return Result<PagedResult<UserDto>>.Fail(AppError.Validation("Validation failed.",
                new Dictionary<string, string[]>
                {
                    ["Page"] = ["Page must be >= 1."],
                    ["PageSize"] = ["PageSize must be in range [1..200]."]
                }));
        }

        var res = await _users.GetListAsync(query, ct);
        return Result<PagedResult<UserDto>>.Ok(res);
    }
}