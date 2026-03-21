using CoreService.Application.Common.Errors;

namespace CoreService.Api.Common;

/// <summary>
/// Единый формат ошибки в HTTP ответах.
/// </summary>
public sealed record ApiErrorResponse(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? Details = null)
{
    public static ApiErrorResponse From(AppError err) =>
        new(err.Code, err.Message, err.Details);
}