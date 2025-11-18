using AutoMapper;
using Gavel.Application.Interfaces;
using Gavel.Domain.Interfaces.Repositories;
using MediatR;

namespace Gavel.Application.Handlers.Bid.PlaceBid;

public class PlaceBidHandler(IBidRepository bidRepository, IBidNotificationService bidNotificationService, IMapper mapper)
    : IRequestHandler<PlaceBidCommand>
{

    public async Task Handle(PlaceBidCommand request, CancellationToken cancellationToken)
    {
        var bid = mapper.Map<Domain.Entities.Bid>(request);
        await bidRepository.CreateAsync(bid);
        await bidNotificationService.NotifyNewBidAsync(bid);
    }
}