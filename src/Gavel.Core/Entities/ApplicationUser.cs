using Microsoft.AspNetCore.Identity;

namespace Gavel.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public virtual ICollection<Bid> Bids { get; set; } = new HashSet<Bid>();
}