using Gavel.Api.Features.Settlements.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace Gavel.Api.Features.Settlements;

public static class SettlementEndpoints
{
    public static void MapSettlementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/settlements");

        group.MapGet("/{id}", GetSettlement);
        
        app.MapGet("/bidders/me/settlements", GetMySettlements);
    }

    private static async Task<IResult> GetSettlement(Guid id, ISettlementService service)
    {
        var settlement = await service.GetSettlementAsync(id);
        return settlement is not null ? Results.Ok(settlement) : Results.NotFound();
    }

    private static async Task<IResult> GetMySettlements(ClaimsPrincipal user, ISettlementService service)
    {
        var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var bidderId))
        {
            return Results.Unauthorized();
        }

        var settlements = await service.GetBidderSettlementsAsync(bidderId);
        return Results.Ok(settlements);
    }
}
