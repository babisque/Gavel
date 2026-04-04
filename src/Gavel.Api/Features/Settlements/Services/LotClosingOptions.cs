namespace Gavel.Api.Features.Settlements.Services;

public record LotClosingOptions
{
    public TimeSpan CheckInterval { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan ErrorBackoff { get; init; } = TimeSpan.FromSeconds(10);
    public int BatchSize { get; init; } = 100;
    
    public TimeSpan OutboxCheckInterval { get; init; } = TimeSpan.FromSeconds(10);
    public int OutboxBatchSize { get; init; } = 50;
}
