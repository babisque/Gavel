namespace Gavel.Api.Infrastructure.Data;

public class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Type { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
}
