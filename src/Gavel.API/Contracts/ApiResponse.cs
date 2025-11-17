using Gavel.Domain;
using System.Text.Json.Serialization;

namespace Gavel.API.Contracts;

public record Meta(
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("pageSize")] int PageSize,
    [property: JsonPropertyName("totalRecords")] int TotalRecords
);

public class ApiResponse<T>
{
    [JsonPropertyName("data")] public T? Data { get; init; }
    [JsonPropertyName("meta")] public Meta? Meta { get; init; }
    [JsonPropertyName("errors")] public List<ErrorItem> Errors { get; init; } = new();

    public static ApiResponse<T> FromData(T? data, Meta? meta = null) => new()
    {
        Data = data,
        Meta = meta,
        Errors = []
    };

    public static ApiResponse<T> FromErrors(IEnumerable<ErrorItem> errors) => new()
    {
        Data = default,
        Meta = null,
        Errors = errors.ToList()
    };
}
