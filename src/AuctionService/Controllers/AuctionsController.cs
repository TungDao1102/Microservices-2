﻿using AuctionService.Dtos;
using AuctionService.Entities;
using AuctionService.Repositories;
using Contracts;
using Mapster;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuctionsController(IAuctionRepository repo, IPublishEndpoint publishEndpoint) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions(string? date)
        {
            return await repo.GetAuctionsAsync(date);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
        {
            var auction = await repo.GetAuctionByIdAsync(id);

            if (auction == null) return NotFound();

            return auction;
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto createAuctionDto)
        {
            var auction = createAuctionDto.Adapt<Auction>();

            auction.Seller = User.Identity?.Name ?? "Unknown user";
            repo.AddAuction(auction);

            var newAuction = auction.Adapt<AuctionDto>();

            var result = await repo.SaveChangesAsync();

            await publishEndpoint.Publish(newAuction.Adapt<AuctionCreated>());

            if (!result) return BadRequest("Could not save changes to DB");

            return CreatedAtAction(nameof(GetAuctionById),
                new { auction.Id }, newAuction);
        }

        [Authorize]
        [HttpPut("{id:guid}")]
        public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto updateAuctionDto)
        {
            var auction = await repo.GetAuctionEntityById(id);

            if (auction == null) return NotFound();

            if (auction.Seller != User.Identity?.Name) return Forbid();

            auction.Item!.Make = updateAuctionDto.Make ?? auction.Item.Make;
            auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
            auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
            auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
            auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

            await publishEndpoint.Publish(auction.Adapt<AuctionUpdated>());

            var result = await repo.SaveChangesAsync();

            if (result) return Ok();

            return BadRequest("Problem saving changes");
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteAuction(Guid id)
        {
            var auction = await repo.GetAuctionEntityById(id);

            if (auction == null) return NotFound();

            if (auction.Seller != User.Identity?.Name) return Forbid();

            repo.RemoveAuction(auction);

            await publishEndpoint.Publish<AuctionDeleted>(new { Id = auction.Id.ToString() });

            var result = await repo.SaveChangesAsync();

            if (!result) return BadRequest("Could not update DB");

            return Ok();
        }
    }
}
