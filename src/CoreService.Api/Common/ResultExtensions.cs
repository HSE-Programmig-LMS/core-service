using CoreService.Application.Common.Errors;
using CoreService.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace CoreService.Api.Common;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
    {
        if (result.IsSuccess)
            return controller.Ok(result.Value);

        var err = result.Error ?? AppError.Internal("Unknown error.");
        return controller.StatusCode(MapStatusCode(err.Code), ApiErrorResponse.From(err));
    }

    public static IActionResult ToCreatedResult<T>(
        this Result<T> result,
        ControllerBase controller,
        string routeName,
        object routeValues)
    {
        if (result.IsSuccess)
            return controller.CreatedAtRoute(routeName, routeValues, result.Value);

        var err = result.Error ?? AppError.Internal("Unknown error.");
        return controller.StatusCode(MapStatusCode(err.Code), ApiErrorResponse.From(err));
    }

    private static int MapStatusCode(string code) => code switch
    {
        // 400
        ErrorCodes.ValidationError => 400,

        // 401
        ErrorCodes.Unauthorized => 401,
        ErrorCodes.InvalidCredentials => 401,
        ErrorCodes.LockedOut => 401,
        ErrorCodes.UserInactive => 401,
        ErrorCodes.InvalidRefreshToken => 401,
        ErrorCodes.InvalidResetToken => 401,

        // 404
        ErrorCodes.UserNotFound => 404,
        ErrorCodes.RoleNotFound => 404,

        // 409
        ErrorCodes.EmailAlreadyExists => 409,

        // fallback
        ErrorCodes.InternalError => 500,
        _ => 500
    };
}