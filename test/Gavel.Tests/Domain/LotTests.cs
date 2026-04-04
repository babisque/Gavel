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
    public async Task Create_InDraft_Succeeds()
    {
        // Act
        var lot = new Lot(LotId, AuctionId, "Classic Car", 50000m);

        // Assert
        await That(lot.State).IsEqualTo(LotState.Draft);
        await That(lot.CurrentPrice).IsEqualTo(50000m);
        await That(lot.Title).IsEqualTo("Classic Car");
    }

    [Test]
    public async Task Create_WithNegativePrice_ThrowsArgumentException()
    {
        // Act
        Action action = () => new Lot(LotId, AuctionId, "Car", -100m);

        // Assert
        await That(action).Throws<ArgumentException>();
    }

    [Test]
    public async Task AddPhoto_MaintainsSequenceAndReorders()
    {
        // Arrange
        var lot = new Lot(LotId, AuctionId, "Car", 1000m);

        // Act
        lot.AddPhoto("p2.jpg", 10);
        lot.AddPhoto("p1.jpg", 1);
        lot.AddPhoto("p3.jpg");

        // Assert
        var photos = lot.Photos.OrderBy(p => p.Order).ToList();
        await That(photos[0].Url).IsEqualTo("p1.jpg");
        await That(photos[0].Order).IsEqualTo(1);
        await That(photos[1].Url).IsEqualTo("p2.jpg");
        await That(photos[1].Order).IsEqualTo(2);
        await That(photos[2].Url).IsEqualTo("p3.jpg");
        await That(photos[2].Order).IsEqualTo(3);
    }

    [Test]
    public async Task AttachPublicNotice_VersionsDocument()
    {
        // Arrange
        var lot = new Lot(LotId, AuctionId, "Car", 1000m);
        var now = DateTimeOffset.UtcNow;

        // Act
        lot.AttachPublicNotice("url/v1", "1.0", now);
        lot.AttachPublicNotice("url/v2", "2.0", now.AddHours(1));

        // Assert
        await That(lot.NoticeHistory).Count().IsEqualTo(2);
        await That(lot.CurrentNotice!.Version).IsEqualTo("2.0");
        await That(lot.CurrentNotice.Url).IsEqualTo("url/v2");
    }

    [Test]
    public async Task Schedule_WithoutPhotos_ThrowsInvalidOperationException()
    {
        // Arrange
        var lot = new Lot(LotId, AuctionId, "Car", 1000m);
        lot.AttachPublicNotice("url", "1.0", DateTimeOffset.UtcNow);

        // Act
        Action action = () => lot.Schedule(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));

        // Assert
        await That(action).Throws<InvalidOperationException>()
            .WithMessage("A lot must have at least one photo before being scheduled.");
    }

    [Test]
    public async Task Schedule_WithoutNotice_ThrowsInvalidOperationException()
    {
        // Arrange
        var lot = new Lot(LotId, AuctionId, "Car", 1000m);
        lot.AddPhoto("photo.jpg");

        // Act
        Action action = () => lot.Schedule(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));

        // Assert
        await That(action).Throws<InvalidOperationException>()
            .WithMessage("A lot must have a Public Notice (Edital) before being scheduled.");
    }

    [Test]
    public async Task PlaceBid_WithLowerAmount_ThrowsInvalidOperationException()
    {
        // Arrange
        var lot = CreateActiveLot();

        // Act
        Action action = () => lot.PlaceBid(Guid.NewGuid(), lot.CurrentPrice - 1, DateTimeOffset.UtcNow);

        // Assert
        await That(action).Throws<InvalidOperationException>()
            .WithMessage("First bid must be at least the starting price.");
    }

    [Test]
    public async Task PlaceBid_InSoftCloseWindow_ExtendsEndTimeUsingConfigurableWindow()
    {
        // Arrange
        var lot = CreateActiveLot();
        lot.SoftCloseWindow = TimeSpan.FromMinutes(5);
        var bidTime = lot.EndTime!.Value.AddMinutes(-4); // Inside the 5 min window
        var newBidAmount = lot.CurrentPrice + 500m;

        // Act
        lot.PlaceBid(Guid.NewGuid(), newBidAmount, bidTime);

        // Assert
        var expectedEndTime = bidTime.AddMinutes(5);
        await That(lot.EndTime).IsEqualTo(expectedEndTime);
        await That(lot.State).IsEqualTo(LotState.Closing);
    }

    [Test]
    public async Task GetPriceBreakdown_ReturnsAccurateValues()
    {
        // Arrange
        var lot = new Lot(LotId, AuctionId, "Car", 10000m);
        lot.SetAdminFees(500m);
        // CurrentPrice = 10000
        // Commission = 10000 * 0.05 = 500
        // Total = 10000 + 500 + 500 = 11000

        // Act
        var breakdown = lot.GetPriceBreakdown();

        // Assert
        await That(breakdown.BidAmount).IsEqualTo(10000m);
        await That(breakdown.CommissionAmount).IsEqualTo(500m);
        await That(breakdown.AdminFees).IsEqualTo(500m);
        await That(breakdown.Total).IsEqualTo(11000m);
    }

    [Test]
    public async Task SetReservePrice_InDraft_Succeeds()
    {
        // Arrange
        var lot = new Lot(LotId, AuctionId, "Car", 1000m);

        // Act
        lot.SetReservePrice(1500m);

        // Assert
        await That(lot.ReservePrice).IsEqualTo(1500m);
    }

    [Test]
    public async Task SetReservePrice_ToNull_Succeeds()
    {
        // Arrange
        var lot = new Lot(LotId, AuctionId, "Car", 1000m);
        lot.SetReservePrice(1500m);

        // Act
        lot.SetReservePrice(null);

        // Assert
        await That(lot.ReservePrice).IsNull();
    }

    [Test]
    public async Task SetReservePrice_LowerThanStartingPrice_ThrowsArgumentException()
    {
        // Arrange
        var lot = new Lot(LotId, AuctionId, "Car", 1000m);

        // Act
        Action action = () => lot.SetReservePrice(500m);

        // Assert
        await That(action).Throws<ArgumentException>()
            .WithMessage("Reserve price cannot be lower than starting price.");
    }

    [Test]
    public async Task SetReservePrice_InActiveState_ThrowsInvalidOperationException()
    {
        // Arrange
        var lot = CreateActiveLot();

        // Act
        Action action = () => lot.SetReservePrice(2000m);

        // Assert
        await That(action).Throws<InvalidOperationException>()
            .WithMessage("Reserve price can only be set in Draft or Scheduled state.");
    }

    [Test]
    public async Task Photos_IsReadOnly_AndThrowsOnExternalMutation()
    {
        // Arrange
        var lot = new Lot(LotId, AuctionId, "Car", 1000m);
        lot.AddPhoto("photo.jpg");

        // Act & Assert
        // Casting to List should fail or mutation on the returned collection should throw
        var photos = lot.Photos;
        
        // This should not compile if we use IReadOnlyCollection, 
        // but if someone tries to cast it to a mutable interface:
        if (photos is System.Collections.IList list)
        {
            Action action = () => list.Add(new Photo("malicious.jpg", 1));
            await That(action).Throws<NotSupportedException>();
        }
    }

    [Test]
    public async Task NoticeHistory_IsReadOnly_AndThrowsOnExternalMutation()
    {
        // Arrange
        var lot = new Lot(LotId, AuctionId, "Car", 1000m);
        lot.AttachPublicNotice("url", "1.0", DateTimeOffset.UtcNow);

        // Act & Assert
        var history = lot.NoticeHistory;
        if (history is System.Collections.IList list)
        {
            Action action = () => list.Clear();
            await That(action).Throws<NotSupportedException>();
        }
    }

    private Lot CreateScheduledLot()
    {
        var lot = new Lot(LotId, AuctionId, "Car", 1000m);
        lot.AddPhoto("photo.jpg");
        lot.AttachPublicNotice("url", "1.0", DateTimeOffset.UtcNow);
        lot.Schedule(DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(2));
        return lot;
    }

    private Lot CreateActiveLot()
    {
        var lot = CreateScheduledLot();
        lot.OpenForBidding(lot.StartTime!.Value);
        return lot;
    }
}
