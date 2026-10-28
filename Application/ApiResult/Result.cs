namespace Application.ApiResult;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;
    public ErrorType? ErrorType { get; private set; }

    private Result() { }

    public static Result<T> Success(T data) => new Result<T>
    {
        IsSuccess = true,
        Data = data
    };

    public static Result<T> Fail(string errorMessage, ErrorType errorType) => new Result<T>
    {
        IsSuccess = false,
        ErrorMessage = errorMessage,
        ErrorType = errorType
    };
}
