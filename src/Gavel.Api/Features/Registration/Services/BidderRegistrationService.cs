namespace Gavel.Api.Features.Registration.Services;

using Gavel.Core.Domain.Registration;
using Gavel.Api.Infrastructure.Data;
using Gavel.Core.Domain.Auctions;
using Gavel.Core.Infrastructure.Logging;
using Gavel.Core.Infrastructure.Notifications;
using Microsoft.Extensions.Logging;

public interface IBidderRegistrationService
{
    Task SubmitBasicInfoAsync(Guid bidderId, ProfileData data);
    Task UploadDocumentsAsync(Guid bidderId, IReadOnlyCollection<Document> documents);
    Task ApproveAsync(Guid bidderId, string adminId);
    Task AcceptTermsAsync(Guid bidderId, string termsVersion, string sourceIp);
    Task RequestActionAsync(Guid bidderId, string reason);
    Task RejectAsync(Guid bidderId, string reason, string adminId);
    Task RegisterForAuctionAsync(Guid bidderId, Guid auctionId);
}

public class BidderRegistrationService(
    GavelDbContext context,
    ITaxIdValidator taxIdValidator,
    IAuditLogger auditLogger,
    INotificationService notificationService,
    TimeProvider timeProvider,
    ILogger<BidderRegistrationService> logger) : IBidderRegistrationService
{
    public async Task SubmitBasicInfoAsync(Guid bidderId, ProfileData data)
    {
        ArgumentNullException.ThrowIfNull(data, nameof(data));
        
        var bidder = await GetBidderOrThrowAsync(bidderId);

        var validationResult = taxIdValidator.Validate(data.TaxId);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"Invalid Tax ID: {validationResult.ErrorMessage}", nameof(data));
        }
        
        bidder.SubmitBasicInfo(data);
        await context.SaveChangesAsync();
    }

    public async Task UploadDocumentsAsync(Guid bidderId, IReadOnlyCollection<Document> documents)
    {
        var bidder = await GetBidderOrThrowAsync(bidderId);
        
        bidder.UploadDocuments(documents);
        await context.SaveChangesAsync();
    }

    public async Task ApproveAsync(Guid bidderId, string adminId)
    {
        var bidder = await GetBidderOrThrowAsync(bidderId);
        
        bidder.Approve();
        await context.SaveChangesAsync();

        try
        {
            await notificationService.SendAsync(bidder.Id, "RegistrationApproved", new { AdminId = adminId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send approval notification for bidder {BidderId}.", bidderId);
        }
    }

    public async Task AcceptTermsAsync(Guid bidderId, string termsVersion, string sourceIp)
    {
        var bidder = await GetBidderOrThrowAsync(bidderId);
        var timestamp = timeProvider.GetUtcNow();
        
        bidder.AcceptTerms(termsVersion, timestamp);
        await context.SaveChangesAsync();
        
        await auditLogger.LogAsync(new AuditRecord(
            bidder.Id,
            "TermsAccepted",
            timestamp,
            $"Version: {termsVersion}, IP: {sourceIp}"
        ));
    }

    public async Task RequestActionAsync(Guid bidderId, string reason)
    {
        var bidder = await GetBidderOrThrowAsync(bidderId);
        
        bidder.RequestAction(reason);
        await context.SaveChangesAsync();
    }

    public async Task RejectAsync(Guid bidderId, string reason, string adminId)
    {
        var bidder = await GetBidderOrThrowAsync(bidderId);
        
        bidder.Reject(reason);
        await context.SaveChangesAsync();

        try
        {
            await notificationService.SendAsync(bidder.Id, "RegistrationRejected", new { Reason = reason, AdminId = adminId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send rejection notification for bidder {BidderId}.", bidderId);
        }
    }

    public async Task RegisterForAuctionAsync(Guid bidderId, Guid auctionId)
    {
        var bidder = await GetBidderOrThrowAsync(bidderId);
        var auction = await context.Auctions.FindAsync(auctionId) 
            ?? throw new KeyNotFoundException($"Auction {auctionId} not found.");

        // For now, the service assumes guarantee checks are handled externally before this call
        // or passes false if not explicitly verified.
        auction.RegisterBidder(bidder, hasPaidGuarantee: false);
        await context.SaveChangesAsync();
    }

    private async Task<Bidder> GetBidderOrThrowAsync(Guid bidderId)
    {
        return await context.Bidders.FindAsync(bidderId) 
            ?? throw new KeyNotFoundException($"Bidder {bidderId} not found.");
    }
}
