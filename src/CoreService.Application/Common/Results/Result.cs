using CoreService.Application.Common.Errors;

namespace CoreService.Application.Common.Results;

public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, AppError? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T? Value { get; }
    public AppError? Error { get; }

    public static Result<T> Ok(T value) => new(true, value, null);

    public static Result<T> Fail(AppError error) => new(false, default, error);
}
