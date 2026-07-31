namespace ProjectManagement.Application.Common;

/// <summary>
/// Why an operation of the logic layer failed. The presentation layer maps these
/// to HTTP status codes, so the services stay free of any web specific types.
/// </summary>
public enum ErrorType
{
    None = 0,
    Validation = 1,
    NotFound = 2,
    Forbidden = 3,
    Conflict = 4
}

/// <summary>Outcome of an operation that returns no value.</summary>
public class Result
{
    protected Result(bool isSuccess, ErrorType errorType, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        ErrorType = errorType;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public ErrorType ErrorType { get; }
    public IReadOnlyList<string> Errors { get; }

    /// <summary>First error message, convenient for single message responses.</summary>
    public string ErrorMessage => Errors.Count > 0 ? Errors[0] : string.Empty;

    public static Result Success() => new(true, ErrorType.None, Array.Empty<string>());

    public static Result Failure(ErrorType type, params string[] errors) => new(false, type, errors);

    public static Result Validation(params string[] errors) => Failure(ErrorType.Validation, errors);
    public static Result NotFound(string message = "The requested item was not found.") => Failure(ErrorType.NotFound, message);
    public static Result Forbidden(string message = "You are not allowed to perform this operation.") => Failure(ErrorType.Forbidden, message);
    public static Result Conflict(string message) => Failure(ErrorType.Conflict, message);
}

/// <summary>Outcome of an operation that returns a value.</summary>
public sealed class Result<T> : Result
{
    private Result(bool isSuccess, T? value, ErrorType errorType, IReadOnlyList<string> errors)
        : base(isSuccess, errorType, errors) => Value = value;

    public T? Value { get; }

    public static Result<T> Success(T value) => new(true, value, ErrorType.None, Array.Empty<string>());

    public new static Result<T> Failure(ErrorType type, params string[] errors) => new(false, default, type, errors);

    public new static Result<T> Validation(params string[] errors) => Failure(ErrorType.Validation, errors);
    public new static Result<T> NotFound(string message = "The requested item was not found.") => Failure(ErrorType.NotFound, message);
    public new static Result<T> Forbidden(string message = "You are not allowed to perform this operation.") => Failure(ErrorType.Forbidden, message);
    public new static Result<T> Conflict(string message) => Failure(ErrorType.Conflict, message);
}
