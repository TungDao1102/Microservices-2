using BiddingService.DTOs;
using BiddingService.Models;
using BiddingService.Services;
using Contracts;
using Mapster;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Entities;

namespace BiddingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BidController(IPublishEndpoint publishEndpoint, GrpcAuctionClient grpcClient) : ControllerBase
    {
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Bid>> PlaceBid(string auctionId, int amount)
        {
            var auction = await DB.Find<Auction>().OneAsync(auctionId);
            if (auction == null)
            {
                auction = grpcClient.GetAuction(auctionId);

                if (auction == null) return BadRequest("Cannot accept bids on this auction at this time");
            }
            if (auction.Finished)
            {
                return BadRequest("Auction is already finished");
            }
            if (amount < auction.ReservePrice)
            {
                return BadRequest("Bid is below reserve price");
            }
            var bid = new Bid
            {
                AuctionId = auctionId,
                Bidder = User?.Identity?.Name ?? string.Empty,
                Amount = amount
            };

            if (auction.AuctionEnd < DateTime.UtcNow)
            {
                bid.BidStatus = BidStatus.Finished;
            }
            else
            {
                var highBid = await DB.Find<Bid>()
               .Match(b => b.AuctionId == auctionId)
               .Sort(b => b.Descending(x => x.Amount))
               .ExecuteFirstAsync();

                if (highBid != null && amount > highBid.Amount || highBid == null)
                {
                    bid.BidStatus = amount > auction.ReservePrice ? BidStatus.Accepted : BidStatus.AcceptedBelowReserve;
                }

                if (highBid != null && highBid.Amount >= bid.Amount)
                {
                    bid.BidStatus = BidStatus.TooLow;
                }
            }

            await bid.SaveAsync();
            await publishEndpoint.Publish(bid.Adapt<BidPlaced>());

            return Ok(bid.Adapt<BidDto>());
        }

        [HttpGet("{auctionId}")]
        public async Task<ActionResult<List<BidDto>>> GetBidsForAuction(string auctionId)
        {
            var bids = await DB.Find<Bid>()
                .Match(a => a.AuctionId == auctionId)
                .Sort(b => b.Descending(a => a.BidTime))
            .ExecuteAsync();

            return bids.Select(x => x.Adapt<BidDto>()).ToList();
        }
    }
}
