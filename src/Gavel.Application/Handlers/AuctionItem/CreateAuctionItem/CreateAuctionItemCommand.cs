using MediatR;

namespace Gavel.Application.Handlers.AuctionItem.CreateAuctionItem;

public class CreateAuctionItemCommand : IRequest<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal InitialPrice { get; set; }
    public DateTime EndTime { get; set; }
}