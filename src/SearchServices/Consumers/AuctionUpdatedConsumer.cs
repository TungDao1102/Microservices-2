using Contracts;
using Mapster;
using MassTransit;
using MongoDB.Entities;
using SearchServices.Models;

namespace SearchServices.Consumers
{
    public class AuctionUpdatedConsumer : IConsumer<AuctionUpdated>
    {
        public async Task Consume(ConsumeContext<AuctionUpdated> context)
        {
            Console.WriteLine("--> Consuming auction updated: " + context.Message.Id);

            var item = context.Message.Adapt<Item>();

            var result = await DB.Update<Item>()
                .Match(a => a.ID == context.Message.Id)
                .ModifyOnly(x => new
                {
                    x.Color,
                    x.Make,
                    x.Model,
                    x.Year,
                    x.Mileage
                }, item)
                .ExecuteAsync();

            if (!result.IsAcknowledged)
                throw new MessageException(typeof(AuctionUpdated), "Problem updating mongoDb");
        }
    }
}
