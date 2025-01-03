

using BiddingService.Models;
using Contracts;
using MassTransit;
using MongoDB.Entities;

namespace BiddingService.Services
{
    public class CheckAuctionFinished(IServiceProvider serviceProvider, ILogger<CheckAuctionFinished> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("CheckAuctionFinished is starting.");

            stoppingToken.Register(() => logger.LogInformation("CheckAuctionFinished is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckAuctionAsync(stoppingToken);
                await Task.Delay(5000, stoppingToken);
            }
        }

        private async Task CheckAuctionAsync(CancellationToken stoppingToken)
        {
            var fisnishAuctions = await DB.Find<Auction>()
                .Match(a => a.AuctionEnd < DateTime.UtcNow && !a.Finished)
                .ExecuteAsync(stoppingToken);

            logger.LogInformation("Found {Count} auctions to finish", fisnishAuctions.Count);

            using var scope = serviceProvider.CreateScope();
            var endpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            foreach (var auction in fisnishAuctions)
            {
                auction.Finished = true;
                await auction.SaveAsync(null, stoppingToken);

                var winningBid = await DB.Find<Bid>()
                    .Match(a => a.AuctionId == auction.ID && a.BidStatus == BidStatus.Accepted)
                    .Sort(x => x.Descending(s => s.Amount))
                    .ExecuteFirstAsync(stoppingToken);

                await endpoint.Publish(new AuctionFinished
                {
                    ItemSold = winningBid != null,
                    AuctionId = auction.ID,
                    Winner = winningBid?.Bidder,
                    Amount = winningBid?.Amount,
                    Seller = auction.Seller
                }, stoppingToken);
            }
        }
    }
}
