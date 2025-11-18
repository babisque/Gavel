using AutoMapper;

namespace Gavel.Application.Profiles;

public class BidMapper : Profile
{
    public BidMapper()
    {
        CreateMap<Handlers.Bid.PlaceBid.PlaceBidCommand, Domain.Entities.Bid>().ReverseMap();
    }
}