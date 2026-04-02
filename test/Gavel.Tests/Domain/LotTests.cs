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
        lot.AddPhoto("p3.jpg"); // default to 3rd because current count is 2+1

        // Assert
        var photos = lot.Photos.ToList();
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
        Action action = () => lot.PlaceBid(lot.CurrentPrice - 1, DateTimeOffset.UtcNow);

        // Assert
        await That(action).Throws<InvalidOperationException>()
            .WithMessage("New bid must be higher than the current price.");
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
        lot.PlaceBid(newBidAmount, bidTime);

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
