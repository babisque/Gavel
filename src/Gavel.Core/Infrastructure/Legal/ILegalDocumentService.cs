using Gavel.Core.Domain.Lots;
using Gavel.Core.Domain.Settlements;
using Gavel.Core.Domain.Bidding;

namespace Gavel.Core.Infrastructure.Legal;

public interface ILegalDocumentService
{
    /// <summary>
    /// Generates the "Ata de Leilão" (Auction Minutes) for a specific lot, 
    /// listing all bids in chronological order for transparency.
    /// </summary>
    Task<byte[]> GenerateLotAtaAsync(Lot lot, List<Bid> bids);

    /// <summary>
    /// Generates the "Nota de Venda" (Sale Note) for a winner, 
    /// pulling the financial breakdown from the settlement.
    /// </summary>
    Task<byte[]> GenerateSaleNoteAsync(Settlement settlement, Lot lot);
}
