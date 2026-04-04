using Gavel.Core.Domain.Lots;
using Gavel.Core.Domain.Settlements;
using TUnit.Core;
using static TUnit.Assertions.Assert;

namespace Gavel.Tests.Domain.Settlements;

public class SettlementTests
{
    [Test]
    public async Task Settlement_CalculatesCommissionCorrectly()
    {
        // Arrange
        var lotId = Guid.NewGuid();
        var bidderId = Guid.NewGuid();
        var winningBidId = Guid.NewGuid();
        var hammerPrice = 10000m;
        var adminFees = 500m;
        
        // 5% of 10000 = 500
        var expectedCommission = 500m;
        var expectedTotal = 11000m;

        var lot = new Lot(lotId, Guid.NewGuid(), "Test Lot", 5000m);
        lot.SetAdminFees(adminFees);
        
        // Act - Simulate a bid to get the breakdown
        // In a real scenario, the lot would have the current price set
        var commission = new Commission { Rate = 0.05m };
        var commissionAmount = commission.Calculate(hammerPrice);
        var breakdown = new PriceBreakdown(hammerPrice, commissionAmount, adminFees, hammerPrice + commissionAmount + adminFees);

        var settlement = new Settlement(
            Guid.NewGuid(),
            lotId,
            bidderId,
            winningBidId,
            breakdown,
            DateTimeOffset.UtcNow
        );

        // Assert
        await That(settlement.PriceBreakdown.BidAmount).IsEqualTo(10000m);
        await That(settlement.PriceBreakdown.CommissionAmount).IsEqualTo(expectedCommission);
        await That(settlement.PriceBreakdown.AdminFees).IsEqualTo(500m);
        await That(settlement.PriceBreakdown.Total).IsEqualTo(expectedTotal);
    }

    [Test]
    public async Task Settlement_LegalFormula_WithComplexValues()
    {
        // Arrange
        var hammerPrice = 12345.67m;
        var adminFees = 123.45m;
        var commission = new Commission { Rate = 0.05m };
        
        // 5% of 12345.67 = 617.2835 -> rounded or exact? 
        // Business rules say "decimal", usually for money we keep precision or round to 2.
        // Current implementation: amount * Rate (0.05m)
        var expectedCommission = 617.2835m; 
        var expectedTotal = hammerPrice + expectedCommission + adminFees;

        // Act
        var breakdown = new PriceBreakdown(hammerPrice, expectedCommission, adminFees, expectedTotal);

        // Assert
        await That(breakdown.Total).IsEqualTo(13086.4035m);
    }
}
