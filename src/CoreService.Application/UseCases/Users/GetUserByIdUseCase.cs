using CoreService.Application.Abstractions.Users;
using CoreService.Application.Common.Errors;
using CoreService.Application.Common.Results;
using CoreService.Application.Contracts.Users;

namespace CoreService.Application.UseCases.Users;

public sealed class GetUserByIdUseCase
{
    private readonly IUserRepository _users;

    public GetUserByIdUseCase(IUserRepository users) => _users = users;

    public async Task<Result<UserDto>> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        return user is null
            ? Result<UserDto>.Fail(new AppError(ErrorCodes.UserNotFound, "User not found."))
            : Result<UserDto>.Ok(user);
    }
}