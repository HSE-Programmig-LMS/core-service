namespace CoreService.Application.Common.Errors;

public static class ErrorCodes
{
    public const string ValidationError = "validation_error";
    public const string InvalidCredentials = "invalid_credentials";
    public const string UserInactive = "user_inactive";
    public const string LockedOut = "locked_out";
    public const string Unauthorized = "unauthorized";
    public const string InternalError = "internal_error";
    public const string UserNotFound = "user_not_found";
    public const string RoleNotFound = "role_not_found";
    public const string EmailAlreadyExists = "email_already_exists";
}
