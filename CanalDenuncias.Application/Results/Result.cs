namespace CanalDenuncias.Application.Results;

public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T? Value { get; }
    public IReadOnlyList<ErrorDto> Errors { get; }

    private Result(bool isSuccess, T? value, IEnumerable<ErrorDto> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors.ToList().AsReadOnly();
    }

    public static Result<T> Success(T value) => new(true, value, []);

    public static Result<T> Failure(params ErrorDto[] errors) => new(false, default, errors);

    public static Result<T> Failure(IEnumerable<ErrorDto> errors) => new(false, default, errors);
}