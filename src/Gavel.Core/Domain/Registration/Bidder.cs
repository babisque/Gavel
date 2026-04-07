namespace Gavel.Core.Domain.Registration;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

public enum BidderState
{
    PendingBasicInfo,
    PendingDocuments,
    UnderReview,
    ActionRequired,
    Approved,
    Rejected,
    Blocked
}

public record ProfileData(string Name, string TaxId, string Email);

public enum DocumentType
{
    OfficialId,
    ProofOfResidence,
    CorporateBylaws
}

public record Document(DocumentType Type, string Url);

/// <summary>
/// Represents a Bidder in the system, managing its lifecycle according to Decree No. 21,981/1932.
/// </summary>
public class Bidder
{
    private readonly List<Document> _documents = [];

    public Guid Id { get; init; } = Guid.NewGuid();
    public BidderState State { get; private set; } = BidderState.PendingBasicInfo;
    public string? Name { get; private set; }
    public string? TaxId { get; private set; }
    public string? Email { get; private set; }
    
    public IReadOnlyCollection<Document> Documents => _documents.AsReadOnly();
    
    public byte[]? RowVersion { get; init; }
    
    public string? StatusReason { get; private set; }

    public string? TermsVersion { get; private set; }
    public DateTimeOffset? TermsAcceptedAt { get; private set; }

    /// <summary>
    /// Explicitly accepts the terms of use for an auction.
    /// Records the version and timestamp for legal auditability.
    /// </summary>
    public void AcceptTerms(string version, DateTimeOffset acceptedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version, nameof(version));
        
        TermsVersion = version;
        TermsAcceptedAt = acceptedAt;
    }

    /// <summary>
    /// Submits basic profile information. 
    /// Required step for legal identification under Decree No. 21,981/1932.
    /// </summary>
    public void SubmitBasicInfo(ProfileData data)
    {
        ArgumentNullException.ThrowIfNull(data, nameof(data));

        if (State != BidderState.PendingBasicInfo && State != BidderState.ActionRequired)
            throw new InvalidStateTransitionException($"Cannot submit basic info from state {State}.");

        Name = data.Name;
        TaxId = data.TaxId;
        Email = data.Email;
        State = BidderState.PendingDocuments;
        StatusReason = null; // Clear stale rejection reasons
    }

    /// <summary>
    /// Uploads mandatory documents for KYC verification.
    /// Complies with the requirement of proving legal capacity.
    /// </summary>
    public void UploadDocuments(IReadOnlyCollection<Document> docs)
    {
        ArgumentNullException.ThrowIfNull(docs, nameof(docs));

        if (docs.Count == 0)
            throw new ArgumentException("At least one document must be provided.", nameof(docs));

        if (State != BidderState.PendingDocuments && State != BidderState.ActionRequired)
            throw new InvalidStateTransitionException($"Cannot upload documents from state {State}.");

        _documents.Clear();
        _documents.AddRange(docs);
        State = BidderState.UnderReview;
        StatusReason = null; // Clear stale rejection reasons
    }

    /// <summary>
    /// Approves the bidder, enabling them to participate in auctions.
    /// Finalizes the qualification process.
    /// </summary>
    public void Approve()
    {
        if (State != BidderState.UnderReview)
            throw new InvalidStateTransitionException("Only bidders under review can be approved.");

        State = BidderState.Approved;
        StatusReason = null;
    }

    /// <summary>
    /// Requests further action from the bidder due to discrepancies.
    /// Internal transition to allow data correction.
    /// </summary>
    public void RequestAction(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("StatusReason is mandatory when requesting action.", nameof(reason));

        if (State != BidderState.UnderReview)
            throw new InvalidStateTransitionException("Action can only be requested for bidders under review.");

        State = BidderState.ActionRequired;
        StatusReason = reason;
    }

    /// <summary>
    /// Rejects the bidder registration.
    /// Irrevocable state reversal is forbidden for Approved bidders to maintain audit integrity.
    /// </summary>
    public void Reject(string reason)
    {
        if (State == BidderState.Approved || State == BidderState.Rejected)
            throw new InvalidStateTransitionException($"Cannot reject a bidder in state {State}.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is mandatory when rejecting a bidder.", nameof(reason));

        State = BidderState.Rejected;
        StatusReason = reason;
    }

    /// <summary>
    /// Blocks the bidder due to delinquency or business rule violation.
    /// This is a restrictive state to prevent participation in auctions.
    /// </summary>
    public void Block(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        
        State = BidderState.Blocked;
        StatusReason = reason;
    }
}

public class InvalidStateTransitionException(string message) : Exception(message);
public class GuaranteeMissingException(string message) : Exception(message);
