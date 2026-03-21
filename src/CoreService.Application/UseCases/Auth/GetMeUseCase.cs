using CoreService.Application.Abstractions.Auth;
using CoreService.Application.Abstractions.Users;
using CoreService.Application.Common.Errors;
using CoreService.Application.Common.Results;
using CoreService.Application.Contracts.Auth;

namespace CoreService.Application.UseCases.Auth;

public sealed class GetMeUseCase
{
    private readonly IUserContext _userContext;
    private readonly IUserRepository _users;

    public GetMeUseCase(IUserContext userContext, IUserRepository users)
    {
        _userContext = userContext;
        _users = users;
    }

    public async Task<Result<MeResponse>> ExecuteAsync(CancellationToken ct = default)
    {
        if (!_userContext.IsAuthenticated || _userContext.UserId is null)
            return Result<MeResponse>.Fail(AppError.Unauthorized());

        var user = await _users.GetByIdAsync(_userContext.UserId.Value, ct);
        if (user is null)
            return Result<MeResponse>.Fail(new AppError(ErrorCodes.UserNotFound, "User not found."));

        var response = new MeResponse(
            UserId: user.UserId,
            Email: user.Email,
            Role: user.Role,
            FirstName: user.FirstName,
            LastName: user.LastName
        );

        return Result<MeResponse>.Ok(response);
    }
}