namespace Gavel.Api.Features.Auctions.Services;

using Gavel.Core.Domain.Lots;
using Gavel.Core.Domain.Bidding;
using Gavel.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public interface ILotManagementService
{
    Task<Guid> CreateLotAsync(Guid auctionId, string title, decimal startingPrice);
    Task AddPhotoAsync(Guid lotId, string url, int? order = null);
    Task AttachPublicNoticeAsync(Guid lotId, string url, string version);
    Task ScheduleLotAsync(Guid lotId, DateTimeOffset start, DateTimeOffset end);
    Task<Lot?> GetLotAsync(Guid lotId);
}

public class LotManagementService(GavelDbContext context, TimeProvider timeProvider) : ILotManagementService
{
    public async Task<Guid> CreateLotAsync(Guid auctionId, string title, decimal startingPrice)
    {
        var lot = new Lot(Guid.NewGuid(), auctionId, title, startingPrice);
        context.Lots.Add(lot);
        await context.SaveChangesAsync();
        return lot.Id;
    }

    public async Task AddPhotoAsync(Guid lotId, string url, int? order = null)
    {
        var lot = await GetLotOrThrowAsync(lotId);
        lot.AddPhoto(url, order);
        await context.SaveChangesAsync();
    }

    public async Task AttachPublicNoticeAsync(Guid lotId, string url, string version)
    {
        var lot = await GetLotOrThrowAsync(lotId);
        lot.AttachPublicNotice(url, version, timeProvider.GetUtcNow());
        await context.SaveChangesAsync();
    }

    public async Task ScheduleLotAsync(Guid lotId, DateTimeOffset start, DateTimeOffset end)
    {
        var lot = await GetLotOrThrowAsync(lotId);
        lot.Schedule(start, end);
        await context.SaveChangesAsync();
    }

    public async Task<Lot?> GetLotAsync(Guid lotId)
    {
        return await context.Lots
            .Include(l => l.Photos)
            .Include(l => l.NoticeHistory)
            .FirstOrDefaultAsync(l => l.Id == lotId);
    }

    private async Task<Lot> GetLotOrThrowAsync(Guid lotId)
    {
        return await GetLotAsync(lotId) 
            ?? throw new KeyNotFoundException($"Lot {lotId} not found.");
    }
}
