using System.Text.Json;
using AutoMapper;
using Gavel.Domain.Constants;
using Gavel.Domain.Entities;
using Gavel.Domain.Enums;
using Gavel.Infrastructure;
using MediatR;
using Quartz;

namespace Gavel.Application.Handlers.AuctionItem.CreateAuctionItem;

public class CreateAuctionItemHandler(
    ApplicationDbContext context,
    ISchedulerFactory schedulerFactory,
    IMapper mapper) : IRequestHandler<CreateAuctionItemCommand, Guid>
{
    public async Task<Guid> Handle(CreateAuctionItemCommand request, CancellationToken cancellationToken)
    {
        var auctionItem = mapper.Map<Domain.Entities.AuctionItem>(request);
        auctionItem.Status = AuctionStatus.Active;
        auctionItem.StartTime = DateTime.UtcNow;
        auctionItem.CurrentPrice = request.InitialPrice;
        
        await context.AuctionItems.AddAsync(auctionItem, cancellationToken);

        var jobIntent = new
        {
            AuctionItemId = auctionItem.Id,
            EndTime = auctionItem.EndTime
        };

        var outboxMessage = new OutboxMessage
        {
            Type = OutboxMessageTypes.ScheduleAuctionClose,
            Payload = JsonSerializer.Serialize(jobIntent)
        };

        await context.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        
        return auctionItem.Id;
    }
}