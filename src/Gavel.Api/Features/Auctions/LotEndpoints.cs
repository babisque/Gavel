namespace Gavel.Api.Features.Auctions;

using Gavel.Api.Features.Auctions.Services;
using Gavel.Core.Domain.Lots;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using FluentValidation;

public static class LotEndpoints
{
    public static void MapLotEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/lots")
            .WithParameterValidation(); // Custom filter for validation

        group.MapPost("/", CreateLot);
        group.MapPut("/{id}/photos", AddPhoto);
        group.MapPut("/{id}/notice", AttachNotice);
        group.MapPost("/{id}/schedule", ScheduleLot);
    }

    private static async Task<Created<Guid>> CreateLot(CreateLotRequest request, ILotManagementService service)
    {
        var id = await service.CreateLotAsync(request.AuctionId, request.Title, request.StartingPrice);
        return TypedResults.Created($"/lots/{id}", id);
    }

    private static async Task<NoContent> AddPhoto(Guid id, AddPhotoRequest request, ILotManagementService service)
    {
        await service.AddPhotoAsync(id, request.Url, request.Order);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> AttachNotice(Guid id, AttachNoticeRequest request, ILotManagementService service)
    {
        await service.AttachPublicNoticeAsync(id, request.Url, request.Version);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> ScheduleLot(Guid id, ScheduleLotRequest request, ILotManagementService service)
    {
        await service.ScheduleLotAsync(id, request.Start, request.End);
        return TypedResults.NoContent();
    }
}

// Request DTOs
public record CreateLotRequest(Guid AuctionId, string Title, decimal StartingPrice);
public record AddPhotoRequest(string Url, int? Order);
public record AttachNoticeRequest(string Url, string Version);
public record ScheduleLotRequest(DateTimeOffset Start, DateTimeOffset End);

// Validators (AOT Compatible - No reflection based scanning)
public class CreateLotRequestValidator : AbstractValidator<CreateLotRequest>
{
    public CreateLotRequestValidator()
    {
        RuleFor(x => x.AuctionId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.StartingPrice).GreaterThan(0);
    }
}

public class AddPhotoRequestValidator : AbstractValidator<AddPhotoRequest>
{
    public AddPhotoRequestValidator()
    {
        RuleFor(x => x.Url).NotEmpty().Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _)).WithMessage("Invalid Photo URL.");
    }
}

public class AttachNoticeRequestValidator : AbstractValidator<AttachNoticeRequest>
{
    public AttachNoticeRequestValidator()
    {
        RuleFor(x => x.Url).NotEmpty().Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _)).WithMessage("Invalid Notice URL.");
        RuleFor(x => x.Version).NotEmpty().MaximumLength(50);
    }
}

public class ScheduleLotRequestValidator : AbstractValidator<ScheduleLotRequest>
{
    public ScheduleLotRequestValidator()
    {
        RuleFor(x => x.Start).NotEmpty();
        RuleFor(x => x.End).NotEmpty().GreaterThan(x => x.Start).WithMessage("End time must be after start time.");
    }
}

// Simple Validation Filter for Minimal APIs (AOT safe)
public static class ValidationExtensions
{
    public static RouteGroupBuilder WithParameterValidation(this RouteGroupBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var argument = context.Arguments.FirstOrDefault(a => 
                a is CreateLotRequest or AddPhotoRequest or AttachNoticeRequest or ScheduleLotRequest);

            if (argument is not null)
            {
                var validator = argument switch
                {
                    CreateLotRequest => (IValidator)new CreateLotRequestValidator(),
                    AddPhotoRequest => (IValidator)new AddPhotoRequestValidator(),
                    AttachNoticeRequest => (IValidator)new AttachNoticeRequestValidator(),
                    ScheduleLotRequest => (IValidator)new ScheduleLotRequestValidator(),
                    _ => null
                };

                if (validator is not null)
                {
                    var validationContext = new ValidationContext<object>(argument);
                    var result = await validator.ValidateAsync(validationContext);

                    if (!result.IsValid)
                    {
                        return Results.ValidationProblem(result.ToDictionary());
                    }
                }
            }

            return await next(context);
        });
    }
}
