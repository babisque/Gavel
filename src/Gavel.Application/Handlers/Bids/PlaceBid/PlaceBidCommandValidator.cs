using FluentValidation;

namespace Gavel.Application.Handlers.Bids.PlaceBid;

public class PlaceBidCommandValidator : AbstractValidator<PlaceBidCommand>
{
    public PlaceBidCommandValidator()
    {
        RuleFor(x => x.AuctionItemId)
            .NotEmpty().WithMessage("Auction item ID is required.");
        
        RuleFor(x => x.BidderId)
            .NotEmpty().WithMessage("Bidder ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Bid amount must be greater than 0.");
    }
}