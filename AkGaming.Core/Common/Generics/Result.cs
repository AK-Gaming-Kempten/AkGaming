namespace AkGaming.Core.Common.Generics;

public class Result {
    public bool IsSuccess { get; }
    public string? Error { get; }

    protected Result(bool success, string? error) {
        IsSuccess = success;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
}

/// <summary>
/// Generic result class that stores a value along with a success flag and an error message
/// </summary>
/// <typeparam name="T">The type of the result value</typeparam>
public class Result<T> : Result {
    public T? Value { get; }

    protected Result(bool success, string? error, T? value)
        : base(success, error) {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, null, value);
    public new static Result<T> Failure(string error) => new(false, error, default);
}
