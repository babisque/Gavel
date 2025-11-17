using AutoMapper;
using Gavel.Domain.Entities;
using Gavel.Application.Handlers.AuctionItem.GetAuctionItems;

namespace Gavel.Application.Profiles;

public class AuctionItemsMapper : Profile
{
    public AuctionItemsMapper()
    {
        CreateMap<AuctionItem, GetAuctionItemsResponse>();
    }
}