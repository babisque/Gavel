using Gavel.Domain.Enums;

namespace Gavel.Domain.Entities;

public class AuctionItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal InitialPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public AuctionStatus Status { get; set; } = AuctionStatus.Pending;
    public byte[] RowVersion { get; set; }
    public virtual ICollection<Bid> Bids { get; set; } = new HashSet<Bid>();
}
