using Microsoft.AspNetCore.Identity;

namespace Gavel.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public virtual ICollection<Bid> Bids { get; set; } = new HashSet<Bid>();
}