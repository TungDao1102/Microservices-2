using Contracts;
using Mapster;
using MassTransit;
using MongoDB.Entities;
using SearchServices.Models;

namespace SearchServices.Consumers
{
	public class AuctionCreatedConsumer : IConsumer<AuctionCreated>
	{
		public async Task Consume(ConsumeContext<AuctionCreated> context)
		{
			await Console.Out.WriteLineAsync("--> Consuming Auction Created: " + context.Message.Id);
			Item item = context.Message.Adapt<Item>();

			if (item.Model == "Foo")
			{
				throw new ArgumentException("Cannot sell cars with name of Foo");
			}
			await item.SaveAsync();
		}
	}
}
