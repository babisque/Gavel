using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Gavel.Api.Infrastructure.Data;
using Gavel.Api.Features.Auctions.Services;
using Gavel.Api.Features.Auctions;
using Gavel.Core.Domain.Lots;
using Gavel.Core.Domain.Registration;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;

using Gavel.Core.Infrastructure.Logging;
using Gavel.Core.Infrastructure.Notifications;
using Gavel.Api.Infrastructure.Logging;
using Gavel.Api.Infrastructure.Notifications;
using Gavel.Api.Features.Registration.Services;

var builder = WebApplication.CreateSlimBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<GavelDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("gaveldb")));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IAuditLogger, AuditLogger>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ITaxIdValidator, BrazilianTaxIdValidator>();
builder.Services.AddScoped<IBidderRegistrationService, BidderRegistrationService>();
builder.Services.AddScoped<ILotManagementService, LotManagementService>();
builder.Services.AddScoped<Gavel.Api.Features.Bidding.Services.IBiddingService, Gavel.Api.Features.Bidding.Services.BiddingService>();

builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var exception = context.HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;

        if (exception is KeyNotFoundException)
        {
            context.ProblemDetails.Status = StatusCodes.Status404NotFound;
            context.ProblemDetails.Title = "Resource Not Found";
            context.ProblemDetails.Detail = exception.Message;
        }
        else if (exception is InvalidOperationException or InvalidStateTransitionException)
        {
            context.ProblemDetails.Status = StatusCodes.Status400BadRequest;
            context.ProblemDetails.Title = "Business Rule Violation";
            context.ProblemDetails.Detail = exception.Message;
        }
        else if (exception is GuaranteeMissingException)
        {
            context.ProblemDetails.Status = StatusCodes.Status403Forbidden;
            context.ProblemDetails.Title = "Qualification Required";
            context.ProblemDetails.Detail = exception.Message;
            context.ProblemDetails.Extensions["errorCode"] = "MANDATORY_GUARANTEE_MISSING";
        }
    };
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapDefaultEndpoints();

// Feature Endpoints
app.MapLotEndpoints();
app.MapHub<Gavel.Api.Features.Bidding.BiddingHub>("/bidding");

app.Run();

// Native AOT Serialization Context
[JsonSerializable(typeof(CreateLotRequest))]
[JsonSerializable(typeof(AddPhotoRequest))]
[JsonSerializable(typeof(AttachNoticeRequest))]
[JsonSerializable(typeof(ScheduleLotRequest))]
[JsonSerializable(typeof(Lot))]
[JsonSerializable(typeof(LotState))]
[JsonSerializable(typeof(Commission))]
[JsonSerializable(typeof(PriceBreakdown))]
[JsonSerializable(typeof(Gavel.Core.Domain.Bidding.ProxyBid))]
[JsonSerializable(typeof(Gavel.Core.Domain.Bidding.Bid))]
[JsonSerializable(typeof(Photo))]
[JsonSerializable(typeof(PublicNotice))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
