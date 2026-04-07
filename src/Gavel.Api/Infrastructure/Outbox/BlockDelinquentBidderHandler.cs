using Gavel.Api.Infrastructure.Data;
using Gavel.Core.Domain.Services;
using Gavel.Api.Features.Registration.Services;

namespace Gavel.Api.Infrastructure.Outbox;

public sealed class BlockDelinquentBidderHandler(
    IBidderRegistrationService bidderService,
    ILogger<BlockDelinquentBidderHandler> logger) : IOutboxHandler
{
    public bool CanHandle(string type) => type == "BlockDelinquentBidder";

    public async Task HandleAsync(OutboxMessage message, GavelDbContext context, CancellationToken ct)
    {
        var bidderId = Guid.Parse(message.Content);
        
        // BidderRegistrationService handles cache invalidation internally
        await bidderService.BlockAsync(bidderId, "Payment delinquency detected by automated monitoring.");
        
        logger.LogWarning("Bidder {BidderId} blocked due to payment default.", bidderId);
    }
}
