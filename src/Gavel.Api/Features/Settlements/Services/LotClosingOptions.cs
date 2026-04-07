namespace Gavel.Api.Features.Settlements.Services;

public record LotClosingOptions
{
    public TimeSpan CheckInterval { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan ErrorBackoff { get; init; } = TimeSpan.FromSeconds(10);
    public int BatchSize { get; init; } = 100;
    
    public TimeSpan OutboxCheckInterval { get; init; } = TimeSpan.FromSeconds(10);
    public int OutboxBatchSize { get; init; } = 50;
    public int MaxOutboxParallelism { get; init; } = 5;

    /// <summary>
    /// Default deadline for settlement payment in business days (e.g., 3 business days as per Decree No. 21,981/1932).
    /// </summary>
    public int SettlementPaymentDeadlineBusinessDays { get; init; } = 3;
}
