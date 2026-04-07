using Gavel.Core.Domain.Auctions;
using Gavel.Core.Domain.Registration;
using Gavel.Api.Features.Registration.Services;
using Gavel.Api.Infrastructure.Data;
using Gavel.Core.Infrastructure.Logging;
using Gavel.Core.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using TUnit.Assertions.Extensions;
using static TUnit.Assertions.Assert;

namespace Gavel.Tests.Domain.Registration;

public class BidderRegistrationWorkflowTests : IDisposable
{
    private readonly IBidderRegistrationService registrationService;
    private readonly ITaxIdValidator taxIdValidator;
    private readonly IAuditLogger auditLogger;
    private readonly TimeProvider timeProvider;
    private readonly INotificationService notificationService;
    private readonly ILogger<BidderRegistrationService> logger;
    private readonly IMemoryCache cache;
    private readonly GavelDbContext context;

    public BidderRegistrationWorkflowTests()
    {
        var options = new DbContextOptionsBuilder<GavelDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        context = new GavelDbContext(options);
        taxIdValidator = Substitute.For<ITaxIdValidator>();
        auditLogger = Substitute.For<IAuditLogger>();
        timeProvider = Substitute.For<TimeProvider>();
        notificationService = Substitute.For<INotificationService>();
        logger = Substitute.For<ILogger<BidderRegistrationService>>();
        cache = Substitute.For<IMemoryCache>();
        
        // Default mock behavior for validator to avoid breaking existing tests
        taxIdValidator.Validate(Arg.Any<string>()).Returns(new ValidationResult(true));

        // Concrete implementation for the service under test
        registrationService = new BidderRegistrationService(context, taxIdValidator, auditLogger, notificationService, timeProvider, logger, cache);
    }

    private static ProfileData DefaultProfile() => new("João Silva", "00011122233", "joao@example.com");

    [Test]
    public async Task Transition_FromPendingBasicInfo_ToPendingDocuments_WhenValidProfileProvided()
    {
        // Arrange
        var bidder = new Bidder();
        context.Bidders.Add(bidder);
        await context.SaveChangesAsync();

        // Act
        await registrationService.SubmitBasicInfoAsync(bidder.Id, DefaultProfile());

        // Assert
        await That(bidder.State).IsEqualTo(BidderState.PendingDocuments);
    }

    [Test]
    public async Task Transition_FromPendingDocuments_ToUnderReview_WhenMandatoryFilesUploaded()
    {
        // Arrange
        var bidder = new Bidder();
        context.Bidders.Add(bidder);
        await context.SaveChangesAsync();
        
        await registrationService.SubmitBasicInfoAsync(bidder.Id, DefaultProfile());
        
        Document[] documents = 
        [ 
            new(DocumentType.OfficialId, "id_url"), 
            new(DocumentType.ProofOfResidence, "residence_url") 
        ];

        // Act
        await registrationService.UploadDocumentsAsync(bidder.Id, documents);

        // Assert
        await That(bidder.State).IsEqualTo(BidderState.UnderReview);
    }

    [Test]
    public async Task Transition_ToApproved_MustPassThroughUnderReview()
    {
        // Arrange
        var bidder = new Bidder();
        context.Bidders.Add(bidder);
        await context.SaveChangesAsync();

        // Act & Assert
        var action = () => registrationService.ApproveAsync(bidder.Id, "Admin_01");
        
        await That(action).Throws<InvalidStateTransitionException>();
    }

    [Test]
    public async Task AuditTrail_ShouldLogAppendOnly_WhenTermsAreAccepted()
    {
        // Arrange
        var bidder = new Bidder();
        context.Bidders.Add(bidder);
        await context.SaveChangesAsync();
        
        await registrationService.SubmitBasicInfoAsync(bidder.Id, DefaultProfile());
        await registrationService.UploadDocumentsAsync(bidder.Id, [new Document(DocumentType.OfficialId, "url")]);
        
        var fixedTime = new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero);
        timeProvider.GetUtcNow().Returns(fixedTime);
        
        const string sourceIp = "192.168.1.1";

        // Act
        await registrationService.AcceptTermsAsync(bidder.Id, "Terms_V1", sourceIp);

        // Assert - Verify audit log receives correct parameters for the append-only record
        await auditLogger.Received(1).LogAsync(Arg.Is<AuditRecord>(r => 
            r.BidderId == bidder.Id &&
            r.Action == "TermsAccepted" &&
            r.Timestamp == fixedTime &&
            r.Metadata.Contains(sourceIp)
        ));
        
        // Assert - Domain state updated
        await That(bidder.TermsVersion).IsEqualTo("Terms_V1");
        await That(bidder.TermsAcceptedAt).IsEqualTo(fixedTime);
    }

    [Test]
    public async Task Transition_FromUnderReview_ToActionRequired_WhenDocumentsAreBlurred()
    {
        // Arrange
        var bidder = new Bidder();
        context.Bidders.Add(bidder);
        await context.SaveChangesAsync();
        
        await registrationService.SubmitBasicInfoAsync(bidder.Id, DefaultProfile());
        await registrationService.UploadDocumentsAsync(bidder.Id, [new Document(DocumentType.OfficialId, "url")]);

        const string reason = "Official ID is blurred. Please re-upload.";

        // Act
        await registrationService.RequestActionAsync(bidder.Id, reason);

        // Assert
        await That(bidder.State).IsEqualTo(BidderState.ActionRequired);
        await That(bidder.StatusReason).IsEqualTo(reason);
    }

    [Test]
    public async Task FinalApproval_ShouldTriggerSignalRNotification()
    {
        // Arrange
        var bidder = new Bidder();
        context.Bidders.Add(bidder);
        await context.SaveChangesAsync();
        
        await registrationService.SubmitBasicInfoAsync(bidder.Id, DefaultProfile());
        await registrationService.UploadDocumentsAsync(bidder.Id, [new Document(DocumentType.OfficialId, "url")]);
        
        // Act
        await registrationService.ApproveAsync(bidder.Id, "Admin_01");

        // Assert
        await notificationService.Received(1).SendAsync(
            bidder.Id, 
            "RegistrationApproved", 
            Arg.Any<object>()
        );
        await That(bidder.State).IsEqualTo(BidderState.Approved);
    }

    [Test]
    public async Task FinalApproval_ShouldNotFail_WhenNotificationServiceFails()
    {
        // Arrange
        var bidder = new Bidder();
        context.Bidders.Add(bidder);
        await context.SaveChangesAsync();
        
        await registrationService.SubmitBasicInfoAsync(bidder.Id, DefaultProfile());
        await registrationService.UploadDocumentsAsync(bidder.Id, [new Document(DocumentType.OfficialId, "url")]);
        
        notificationService.SendAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<object>())
            .Returns(Task.FromException(new Exception("Network error")));

        // Act
        await registrationService.ApproveAsync(bidder.Id, "Admin_01");

        // Assert - DB state is still updated
        await That(bidder.State).IsEqualTo(BidderState.Approved);
        // Error is logged
        logger.ReceivedWithAnyArgs(1).Log(LogLevel.Error, default, default!, default, default!);
    }

    [Test]
    public async Task Registration_ShouldFail_WhenGuaranteeIsRequiredButNotProvided()
    {
        // Arrange
        var bidder = new Bidder();
        context.Bidders.Add(bidder);
        await context.SaveChangesAsync();
        
        await registrationService.SubmitBasicInfoAsync(bidder.Id, DefaultProfile());
        await registrationService.UploadDocumentsAsync(bidder.Id, [new Document(DocumentType.OfficialId, "url")]);
        await registrationService.ApproveAsync(bidder.Id, "Admin_01");

        var auction = new Auction(Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1)) { RequiresGuarantee = true };
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        // Act & Assert
        var action = () => registrationService.RegisterForAuctionAsync(bidder.Id, auction.Id);
        await That(action).Throws<GuaranteeMissingException>();
    }

    [Test]
    public async Task AuditLog_MustIncludeIpAddress_ForLegalCompliance()
    {
        // Arrange
        var bidder = new Bidder();
        context.Bidders.Add(bidder);
        await context.SaveChangesAsync();
        
        const string sourceIp = "200.150.10.1";

        // Act
        await registrationService.AcceptTermsAsync(bidder.Id, "Edital_V2", sourceIp);

        // Assert
        await auditLogger.Received(1).LogAsync(Arg.Is<AuditRecord>(r => r.Metadata.Contains(sourceIp)));
    }

    [Test]
    public async Task SubmitBasicInfo_ShouldThrow_WhenDataIsNull()
    {
        // Arrange
        var bidder = new Bidder();
        context.Bidders.Add(bidder);
        await context.SaveChangesAsync();

        // Act & Assert
        var action = () => registrationService.SubmitBasicInfoAsync(bidder.Id, null!);
        await That(action).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task UploadDocuments_ShouldThrow_WhenDocsIsNull()
    {
        // Arrange
        var bidder = new Bidder();
        context.Bidders.Add(bidder);
        await context.SaveChangesAsync();
        
        await registrationService.SubmitBasicInfoAsync(bidder.Id, DefaultProfile());

        // Act & Assert
        var action = () => registrationService.UploadDocumentsAsync(bidder.Id, null!);
        await That(action).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Reject_ShouldThrow_WhenAlreadyApproved()
    {
        // Arrange
        var bidder = new Bidder();
        context.Bidders.Add(bidder);
        await context.SaveChangesAsync();
        
        await registrationService.SubmitBasicInfoAsync(bidder.Id, DefaultProfile());
        await registrationService.UploadDocumentsAsync(bidder.Id, [new Document(DocumentType.OfficialId, "url")]);
        await registrationService.ApproveAsync(bidder.Id, "Admin_01");

        // Act & Assert
        var action = () => registrationService.RejectAsync(bidder.Id, "Late rejection", "Admin_01");
        await That(action).Throws<InvalidStateTransitionException>();
    }

    [Test]
    public async Task Reject_ShouldThrow_WhenReasonIsEmpty()
    {
        // Arrange
        var bidder = new Bidder();
        context.Bidders.Add(bidder);
        await context.SaveChangesAsync();
        
        await registrationService.SubmitBasicInfoAsync(bidder.Id, DefaultProfile());
        await registrationService.UploadDocumentsAsync(bidder.Id, [new Document(DocumentType.OfficialId, "url")]);

        // Act & Assert
        var action = () => registrationService.RejectAsync(bidder.Id, " ", "Admin_01");
        await That(action).Throws<ArgumentException>();
    }

    [Test]
    public async Task SubmitBasicInfo_ShouldClearStaleStatusReason_WhenCorrectingData()
    {
        // Arrange
        var bidder = new Bidder();
        context.Bidders.Add(bidder);
        await context.SaveChangesAsync();
        
        await registrationService.SubmitBasicInfoAsync(bidder.Id, DefaultProfile());
        await registrationService.UploadDocumentsAsync(bidder.Id, [new Document(DocumentType.OfficialId, "url")]);
        await registrationService.RequestActionAsync(bidder.Id, "Invalid Name");
        
        // Act
        await registrationService.SubmitBasicInfoAsync(bidder.Id, DefaultProfile());

        // Assert
        await That(bidder.StatusReason).IsNull();
    }

    [Test]
    public async Task UploadDocuments_ShouldClearStaleStatusReason_WhenReuploading()
    {
        // Arrange
        var bidder = new Bidder();
        context.Bidders.Add(bidder);
        await context.SaveChangesAsync();
        
        await registrationService.SubmitBasicInfoAsync(bidder.Id, DefaultProfile());
        await registrationService.UploadDocumentsAsync(bidder.Id, [new Document(DocumentType.OfficialId, "url")]);
        await registrationService.RequestActionAsync(bidder.Id, "Blurred Document");

        // Act
        await registrationService.UploadDocumentsAsync(bidder.Id, [new Document(DocumentType.OfficialId, "clear_url")]);

        // Assert
        await That(bidder.StatusReason).IsNull();
    }

    [Test]
    public async Task UploadDocuments_ShouldThrow_WhenCollectionIsEmpty()
    {
        // Arrange
        var bidder = new Bidder();
        context.Bidders.Add(bidder);
        await context.SaveChangesAsync();
        
        await registrationService.SubmitBasicInfoAsync(bidder.Id, DefaultProfile());

        // Act & Assert
        var action = () => registrationService.UploadDocumentsAsync(bidder.Id, []);
        await That(action).Throws<ArgumentException>();
    }

    [Test]
    public async Task RegisterForAuction_ShouldThrow_WhenBidderIsAlreadyRegistered()
    {
        // Arrange
        var bidder = new Bidder();
        context.Bidders.Add(bidder);
        await context.SaveChangesAsync();
        
        await registrationService.SubmitBasicInfoAsync(bidder.Id, DefaultProfile());
        await registrationService.UploadDocumentsAsync(bidder.Id, [new Document(DocumentType.OfficialId, "url")]);
        await registrationService.ApproveAsync(bidder.Id, "Admin_01");

        var auction = new Auction(Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1)) { RequiresGuarantee = false };
        context.Auctions.Add(auction);
        await context.SaveChangesAsync();

        // Initial registration
        await registrationService.RegisterForAuctionAsync(bidder.Id, auction.Id);

        // Act & Assert
        var action = () => registrationService.RegisterForAuctionAsync(bidder.Id, auction.Id);
        await That(action).Throws<InvalidOperationException>();
    }

    public void Dispose()
    {
        context.Dispose();
    }
}
