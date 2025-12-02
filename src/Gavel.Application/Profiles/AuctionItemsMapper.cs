using AutoMapper;
using Gavel.Application.Handlers.AuctionItem.CreateAuctionItem;
using Gavel.Application.Handlers.AuctionItem.GetAuctionItemById;
using Gavel.Domain.Entities;
using Gavel.Application.Handlers.AuctionItem.GetAuctionItems;

namespace Gavel.Application.Profiles;

public class AuctionItemsMapper : Profile
{
    public AuctionItemsMapper()
    {
        CreateMap<CreateAuctionItemCommand, AuctionItem>()
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => new byte[8]));
        CreateMap<AuctionItem, GetAuctionItemsResponse>();
        CreateMap<AuctionItem, GetAuctionItemByIdResponse>();
        CreateMap<Bid, BidResponse>();
    }
}