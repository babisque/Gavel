using AutoMapper;

namespace Gavel.Application.Profiles;

public class BidMapper : Profile
{
    public BidMapper()
    {
        CreateMap<Domain.Entities.Bid, Handlers.Bid.PlaceBid.PlaceBidCommand>();
    }
}