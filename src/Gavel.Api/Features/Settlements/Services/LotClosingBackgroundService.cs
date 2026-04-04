using Gavel.Api.Features.Settlements.Services;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Gavel.Api.Features.Settlements.Services;

public class LotClosingBackgroundService(
    IServiceProvider serviceProvider,
    IOptions<LotClosingOptions> options,
    ILogger<LotClosingBackgroundService> logger) : BackgroundService
{
    private readonly LotClosingOptions _options = options.Value;
    
    private readonly AsyncRetryPolicy _retryPolicy = Policy
        .Handle<Exception>(ex => ex is not OperationCanceledException)
        .WaitAndRetryAsync(5, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            (exception, timeSpan, retryCount, context) =>
            {
                logger.LogWarning(exception, "Transient error in LotClosing Engine. Retry {RetryCount} in {Delay}ms.", retryCount, timeSpan.TotalMilliseconds);
            });

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Lot Closing Background Service is starting with interval {Interval} and batch size {BatchSize}.", _options.CheckInterval, _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    using var scope = serviceProvider.CreateScope();
                    var settlementService = scope.ServiceProvider.GetRequiredService<ISettlementService>();
                    
                    await settlementService.ProcessExpiredLotsAsync(stoppingToken);
                });

                await Task.Delay(_options.CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Critical error in LotClosing Engine. Backing off for {Delay}ms.", _options.ErrorBackoff.TotalMilliseconds);
                await Task.Delay(_options.ErrorBackoff, stoppingToken);
            }
        }

        logger.LogInformation("Lot Closing Background Service is stopping.");
    }
}
