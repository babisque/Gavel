using System.Text.Json;
using AutoMapper;
using Gavel.Domain.Entities;
using Gavel.Domain.Enums;
using Gavel.Domain.Interfaces;
using Gavel.Infrastructure.Jobs;
using MediatR;
using Quartz;

namespace Gavel.Application.Handlers.AuctionItem.CreateAuctionItem;

public class CreateAuctionItemHandler(IUnitOfWork unitOfWork,
    ISchedulerFactory schedulerFactory,
    IMapper mapper) : IRequestHandler<CreateAuctionItemCommand, Guid>
{
    public async Task<Guid> Handle(CreateAuctionItemCommand request, CancellationToken cancellationToken)
    {
        var auctionItem = mapper.Map<Domain.Entities.AuctionItem>(request);
        auctionItem.Status = AuctionStatus.Active;
        auctionItem.StartTime = DateTime.UtcNow;
        auctionItem.CurrentPrice = request.InitialPrice;
        
        await unitOfWork.AuctionItems.CreateAsync(auctionItem);

        var jobIntent = new
        {
            AuctionItemId = auctionItem.Id,
            EndTime = auctionItem.EndTime
        };

        var message = new OutboxMessage
        {
            Type = "ScheduleAuctionClose",
            Payload = JsonSerializer.Serialize(jobIntent)
        };

        await unitOfWork.CompleteAsync(cancellationToken);
        
        return auctionItem.Id;
    }
}