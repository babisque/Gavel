using MediatR;

namespace Gavel.Application.Handlers.AuctionItem.GetAuctionItems;

public class GetAuctionItemsQuery : IRequest<(List<GetAuctionItemsResponse> Items, int TotalCount)>
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 10;
}