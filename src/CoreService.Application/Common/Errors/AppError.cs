namespace CoreService.Application.Common.Errors;

public sealed record AppError(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? Details = null)
{
    public static AppError Validation(string message, IReadOnlyDictionary<string, string[]> details) =>
        new(ErrorCodes.ValidationError, message, details);

    public static AppError InvalidCredentials() =>
        new(ErrorCodes.InvalidCredentials, "Invalid email or password.");

    public static AppError UserInactive() =>
        new(ErrorCodes.UserInactive, "User is inactive.");

    public static AppError LockedOut() =>
        new(ErrorCodes.LockedOut, "User is locked out.");

    public static AppError Unauthorized() =>
        new(ErrorCodes.Unauthorized, "Unauthorized.");

    public static AppError Internal(string message = "Internal error.") =>
        new(ErrorCodes.InternalError, message);
}
