using Gavel.Api.Infrastructure.Data;
using Gavel.Core.Domain.Services;
using Gavel.Core.Domain.Settlements;
using Gavel.Core.Infrastructure.Legal;
using Gavel.Core.Infrastructure.Logging;
using Gavel.Core.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace Gavel.Api.Infrastructure.Outbox;

public sealed class GenerateDocumentsHandler(
    ILegalDocumentService documentService,
    IStorageService storageService,
    IAuditLogger auditLogger,
    TimeProvider timeProvider) : IOutboxHandler
{
    public bool CanHandle(string type) => type is "GenerateLegalDocuments" or "GenerateAuctionMinutes";

    public async Task HandleAsync(OutboxMessage message, GavelDbContext context, CancellationToken ct)
    {
        if (message.Type == "GenerateLegalDocuments")
        {
            await HandleSaleNoteAsync(message, context, ct);
        }
        else if (message.Type == "GenerateAuctionMinutes")
        {
            await HandleAuctionMinutesAsync(message, context, ct);
        }
    }

    private async Task HandleSaleNoteAsync(OutboxMessage message, GavelDbContext context, CancellationToken ct)
    {
        var settlementId = Guid.Parse(message.Content);

        var settlement = await context.Settlements.FirstOrDefaultAsync(s => s.Id == settlementId, ct)
            ?? throw new InvalidOperationException($"Settlement {settlementId} not found.");

        if (settlement != null)
        {
            var lot = await context.Lots.FindAsync([settlement.LotId], ct)
                ?? throw new InvalidOperationException($"Lot {settlement.LotId} not found.");
            if (lot != null)
            {
                var pdfContent = await documentService.GenerateSaleNoteAsync(settlement, lot);
                var fileName = $"sale_note_{settlement.Id}.pdf";
                var url = await storageService.UploadAsync(fileName, pdfContent, ct);
                
                settlement.SetSaleNoteUrl(url);
                
                await auditLogger.LogAsync(new AuditRecord(
                    settlement.BidderId,
                    "SaleNoteGenerated",
                    timeProvider.GetUtcNow(),
                    $"Settlement: {settlement.Id}, URL: {url}"
                ));

                // Note: The OutboxProcessor handles SaveChangesAsync at the end of the batch.
            }
        }
    }

    private async Task HandleAuctionMinutesAsync(OutboxMessage message, GavelDbContext context, CancellationToken ct)
    {
        var lotId = Guid.Parse(message.Content);

        var lot = await context.Lots.FindAsync([lotId], ct);
        if (lot != null)
        {
            var bids = await context.Bids
                .Where(b => b.LotId == lotId)
                .OrderBy(b => b.Timestamp)
                .ToListAsync(ct);

            var pdfContent = await documentService.GenerateLotAtaAsync(lot, bids);
            var fileName = $"auction_minutes_{lot.Id}.pdf";
            var url = await storageService.UploadAsync(fileName, pdfContent, ct);
            
            await auditLogger.LogAsync(new AuditRecord(
                Guid.Empty,
                "AuctionMinutesGenerated",
                timeProvider.GetUtcNow(),
                $"Lot: {lot.Id}, URL: {url}"
            ));
        }
    }
}
