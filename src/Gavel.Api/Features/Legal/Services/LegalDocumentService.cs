using System.Text;
using Gavel.Core.Domain.Lots;
using Gavel.Core.Domain.Settlements;
using Gavel.Core.Domain.Bidding;
using Gavel.Core.Infrastructure.Legal;

namespace Gavel.Api.Features.Legal.Services;

public class LegalDocumentService(TimeProvider timeProvider) : ILegalDocumentService
{
    public Task<byte[]> GenerateLotAtaAsync(Lot lot, List<Bid> bids)
    {
        var now = timeProvider.GetUtcNow();
        var sb = new StringBuilder();
        sb.AppendLine("ATA DE LEILÃO (AUCTION MINUTES)");
        sb.AppendLine($"Lot ID: {lot.Id}");
        sb.AppendLine($"Title: {lot.Title}");
        sb.AppendLine($"Generation Date: {now:O}");
        sb.AppendLine("------------------------------------------");
        sb.AppendLine("BID HISTORY (CHRONOLOGICAL):");
        
        var orderedBids = bids.OrderBy(b => b.Timestamp).ToList();
        foreach (var bid in orderedBids)
        {
            sb.AppendLine($"[{bid.Timestamp:O}] Bidder: {bid.BidderId} | Amount: {bid.Amount:C}");
        }

        sb.AppendLine("------------------------------------------");
        sb.AppendLine($"Final Outcome: {lot.State}");
        sb.AppendLine($"Final Price: {lot.CurrentPrice:C}");

        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    public Task<byte[]> GenerateSaleNoteAsync(Settlement settlement, Lot lot)
    {
        var sb = new StringBuilder();
        sb.AppendLine("NOTA DE VENDA (SALE NOTE)");
        sb.AppendLine($"Lot: {lot.Id} - {lot.Title}");
        sb.AppendLine($"Winning Bidder: {settlement.BidderId}");
        sb.AppendLine($"Settlement Date: {settlement.IssuedAt:O}");
        sb.AppendLine("------------------------------------------");
        
        // Formula: Vt = Va + 0.05Va + Fees + Taxes
        sb.AppendLine($"Hammer Price (Va): {settlement.PriceBreakdown.BidAmount:C}");
        sb.AppendLine($"Auctioneer Commission (0.05Va): {settlement.PriceBreakdown.CommissionAmount:C}");
        sb.AppendLine($"Administrative Fees: {settlement.PriceBreakdown.AdminFees:C}");
        sb.AppendLine($"Total Due (Vt): {settlement.PriceBreakdown.Total:C}");
        
        sb.AppendLine("------------------------------------------");
        sb.AppendLine($"Digital Signature: {settlement.DigitalSignature}");
        sb.AppendLine($"Settlement Status: {settlement.Status}");

        return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
    }
}
