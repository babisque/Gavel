using Gavel.Core.Domain.Lots;
using Gavel.Core.Domain.Settlements;
using TUnit.Core;
using static TUnit.Assertions.Assert;

namespace Gavel.Tests.Domain.Settlements;

public class SettlementHardeningTests
{
    private Settlement CreatePendingSettlement()
    {
        return new Settlement(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new PriceBreakdown(1000, 50, 10, 1060),
            DateTimeOffset.UtcNow
        );
    }

    [Test]
    public async Task ApplySignature_WithValidSignature_Succeeds()
    {
        // Arrange
        var settlement = CreatePendingSettlement();
        var signature = "valid-signature-hash";

        // Act
        settlement.ApplySignature(signature);

        // Assert
        await That(settlement.Status).IsEqualTo(SettlementStatus.Signed);
        await That(settlement.DigitalSignature).IsEqualTo(signature);
    }

    [Test]
    public async Task ApplySignature_WithNullOrEmpty_ThrowsArgumentException()
    {
        // Arrange
        var settlement = CreatePendingSettlement();

        // Act & Assert
        Action action1 = () => settlement.ApplySignature(null!);
        Action action2 = () => settlement.ApplySignature("");
        Action action3 = () => settlement.ApplySignature("   ");

        await That(action1).Throws<ArgumentException>();
        await That(action2).Throws<ArgumentException>();
        await That(action3).Throws<ArgumentException>();
    }

    [Test]
    public async Task Cancel_PendingSettlement_Succeeds()
    {
        // Arrange
        var settlement = CreatePendingSettlement();
        var reason = "Bidder default";

        // Act
        settlement.Cancel(reason);

        // Assert
        await That(settlement.Status).IsEqualTo(SettlementStatus.Canceled);
        await That(settlement.CancellationReason).IsEqualTo(reason);
    }

    [Test]
    public async Task Cancel_SignedSettlement_WithoutOverride_ThrowsInvalidOperationException()
    {
        // Arrange
        var settlement = CreatePendingSettlement();
        settlement.ApplySignature("signed");

        // Act
        Action action = () => settlement.Cancel("Error in values");

        // Assert
        await That(action).Throws<InvalidOperationException>()
            .WithMessage("A signed settlement cannot be canceled without an administrative override.");
    }

    [Test]
    public async Task Cancel_SignedSettlement_WithOverride_Succeeds()
    {
        // Arrange
        var settlement = CreatePendingSettlement();
        settlement.ApplySignature("signed");
        var reason = "Legal challenge approved";

        // Act
        settlement.Cancel(reason, isAdministrativeOverride: true);

        // Assert
        await That(settlement.Status).IsEqualTo(SettlementStatus.Canceled);
        await That(settlement.CancellationReason).IsEqualTo(reason);
    }
}
