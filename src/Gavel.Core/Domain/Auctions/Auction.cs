namespace Gavel.Core.Domain.Auctions;

public class Auction(Guid id, DateTimeOffset startDateTime, DateTimeOffset endDateTime)
{
    public Guid Id { get; init; } = id;
    public DateTimeOffset StartDateTime { get; init; } = startDateTime;
    public DateTimeOffset EndDateTime { get; init; } = endDateTime;
}
