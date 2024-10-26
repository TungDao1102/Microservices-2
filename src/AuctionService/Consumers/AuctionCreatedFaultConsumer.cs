using Contracts;
using MassTransit;

namespace AuctionService.Consumers
{
	public class AuctionCreatedFaultConsumer : IConsumer<Fault<AuctionCreated>>
	{
		public async Task Consume(ConsumeContext<Fault<AuctionCreated>> context)
		{
			await Console.Out.WriteLineAsync("--> Consuming faulty upsert");

			var exception = context.Message.Exceptions.First();
			if (exception.ExceptionType == typeof(System.ArgumentException).ToString())
			{
				context.Message.Message.Model = "FooBar";
				await context.Publish(context.Message.Message);
			}
			else
			{
				await Console.Out.WriteLineAsync("Not an argument exception - update error dashboard somewhere");
			}
		}
	}
}
