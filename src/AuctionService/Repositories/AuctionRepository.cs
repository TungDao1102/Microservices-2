using AuctionService.Data;
using AuctionService.Dtos;
using AuctionService.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Repositories
{
    public class AuctionRepository(AuctionDbContext context) : IAuctionRepository
    {
        public void AddAuction(Auction auction)
        {
            context.Auctions.Add(auction);
        }

        public async Task<AuctionDto?> GetAuctionByIdAsync(Guid id)
        {
            return await context.Auctions
            .ProjectToType<AuctionDto>()
            .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Auction?> GetAuctionEntityById(Guid id)
        {
            return await context.Auctions
                .Include(x => x.Item)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<AuctionDto>> GetAuctionsAsync(string? date)
        {
            var query = context.Auctions
                    .OrderBy(x => x.Item == null)
                    .ThenBy(x => x.Item!.Make)
                    .AsQueryable();

            if (!string.IsNullOrEmpty(date))
            {
                query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
            }

            return await query.ProjectToType<AuctionDto>().ToListAsync();
        }

        public void RemoveAuction(Auction auction)
        {
            context.Auctions.Remove(auction);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await context.SaveChangesAsync() > 0;
        }
    }
}
