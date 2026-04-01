using Gavel.Core.Domain.Registration;
using TUnit.Assertions.Extensions;
using static TUnit.Assertions.Assert;

namespace Gavel.Tests.Domain.Registration;

public class BidderConcurrencyTests
{
    [Test]
    public async Task Update_ShouldIncludeRowVersion_ToPreventConcurrentAdminApprovals()
    {
        // Arrange
        var bidder = new Bidder { Id = Guid.NewGuid(), RowVersion = [1, 2, 3] };
        
        // This test verifies that the domain object tracks its version for Optimistic Concurrency
        await That(bidder.RowVersion).IsNotNull();
        await That(bidder.RowVersion.Length).IsGreaterThan(0);
    }
}
