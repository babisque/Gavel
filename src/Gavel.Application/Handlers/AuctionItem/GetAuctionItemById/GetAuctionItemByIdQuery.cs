using MediatR;

namespace Gavel.Application.Handlers.AuctionItem.GetAuctionItemById;

public class GetAuctionItemByIdQuery : IRequest<GetAuctionItemByIdResponse>
{
    public Guid Id { get; set; }
}