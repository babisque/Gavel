using AutoMapper;
using Gavel.Application.Handlers.Bids.PlaceBid;

namespace Gavel.Application.Profiles;

public class BidMapper : Profile
{
    public BidMapper()
    {
        CreateMap<PlaceBidCommand, Domain.Entities.Bid>().ReverseMap();
    }
}