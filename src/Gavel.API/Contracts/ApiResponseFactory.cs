using Gavel.Domain;

namespace Gavel.API.Contracts;

public class ApiResponseFactory
{
    public static ApiResponse<T> Success<T>(T? data, Meta? meta = null)
        => ApiResponse<T>.FromData(data, meta);

    public static ApiResponse<T> Failure<T>(IEnumerable<ErrorItem> errors)
        => ApiResponse<T>.FromErrors(errors);

    public static ApiResponse<T> Failure<T>(string code, string message, string? field = null)
        => ApiResponse<T>.FromErrors([new ErrorItem(code, field, message)]);
}
