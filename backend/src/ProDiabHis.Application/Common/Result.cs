namespace ProDiabHis.Application.Common;

/// <summary>Ket qua tra ve tu handler, bao gom thanh cong hoac loi</summary>
public class Result<T>
{
    public bool IsSuccess { get; private init; }
    public T? Value { get; private init; }
    public string? ErrorCode { get; private init; }
    public string? ErrorMessage { get; private init; }
    public object? ErrorDetails { get; private init; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };

    public static Result<T> Failure(string errorCode, string errorMessage, object? details = null) =>
        new() { IsSuccess = false, ErrorCode = errorCode, ErrorMessage = errorMessage, ErrorDetails = details };
}

public class Result
{
    public bool IsSuccess { get; private init; }
    public string? ErrorCode { get; private init; }
    public string? ErrorMessage { get; private init; }
    public object? ErrorDetails { get; private init; }

    public static Result Success() => new() { IsSuccess = true };

    public static Result Failure(string errorCode, string errorMessage, object? details = null) =>
        new() { IsSuccess = false, ErrorCode = errorCode, ErrorMessage = errorMessage, ErrorDetails = details };
}
