using Gavel.Core.Domain.Lots;
using NSubstitute;
using TUnit.Core;
using static TUnit.Assertions.Assert;

namespace Gavel.Tests.Domain;

public class LotTests
{
    private static readonly Guid AuctionId = Guid.NewGuid();
    private static readonly Guid LotId = Guid.NewGuid();

    [Test]
    public async Task TransitionTo_DraftToActive_ThrowsInvalidOperationException()
    {
        // Arrange
        var lot = new Lot(LotId, AuctionId, "Car", 1000m);

        // Act & Assert
        var action = () => lot.TransitionTo(LotState.Active);
        await That(action).ThrowsException();
    }

    [Test]
    public async Task TransitionTo_DraftToScheduledToActive_Succeeds()
    {
        // Arrange
        var lot = new Lot(LotId, AuctionId, "Car", 1000m);

        // Act
        lot.TransitionTo(LotState.Scheduled);
        lot.TransitionTo(LotState.Active);

        // Assert
        await That(lot.State).IsEqualTo(LotState.Active);
    }

    [Test]
    public async Task CalculateTotalPrice_CalculatesWith5PercentCommissionAndFees()
    {
        // Arrange
        var lot = new Lot(LotId, AuctionId, "Car", 1000m) 
        { 
            AdminFees = 100m 
        };
        // CurrentPrice is 1000m (starting price)
        // Vt = 1000 + (1000 * 0.05) + 100 = 1150
        
        // Act
        var totalPrice = lot.CalculateTotalPrice();

        // Assert
        await That(totalPrice).IsEqualTo(1150m);
    }

    [Test]
    public async Task PlaceBid_InSoftCloseWindow_ExtendsEndTime()
    {
        // Arrange
        var initialEndTime = DateTimeOffset.UtcNow.AddMinutes(2);
        var lot = new Lot(LotId, AuctionId, "Car", 1000m);
        lot.SetEndTime(initialEndTime);
        lot.TransitionTo(LotState.Scheduled);
        lot.TransitionTo(LotState.Active);

        var bidTime = initialEndTime.AddSeconds(-30); // 30s before end
        var timeProvider = Substitute.For<TimeProvider>();
        
        // Act
        lot.PlaceBid(1500m, bidTime, timeProvider);

        // Assert
        var expectedEndTime = bidTime.AddMinutes(3); // Soft close: +3 minutes
        await That(lot.EndTime).IsEqualTo(expectedEndTime);
    }

    [Test]
    public async Task CommissionRate_IsExactly5Percent()
    {
        // Arrange
        var lot = new Lot(LotId, AuctionId, "Car", 1000m);

        // Act
        var rate = lot.CommissionRate;

        // Assert
        await That(rate).IsEqualTo(0.05m);
    }
}
