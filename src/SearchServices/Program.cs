using MassTransit;
using Polly;
using Polly.Extensions.Http;
using SearchServices.Consumers;
using SearchServices.Data;
using SearchServices.Services;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpClient<AuctionSvcHttpClient>().AddPolicyHandler(GetPolicy());
builder.Services.AddMassTransit(options =>
{
	// options.SetKebabCaseEndpointNameFormatter();
	options.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();
	options.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("search", false));
	options.UsingRabbitMq((context, config) =>
	{
		config.ReceiveEndpoint("search-auction-created", e =>
		{
			e.UseMessageRetry(r => r.Interval(5, 5));

			e.ConfigureConsumer<AuctionCreatedConsumer>(context);
		});

		config.ConfigureEndpoints(context);
	});
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Lifetime.ApplicationStarted.Register(async () =>
{
	await Policy.Handle<TimeoutException>()
		.WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(10))
		.ExecuteAndCaptureAsync(async () => await app.InitDb());
});

app.Run();

// auto recall api if has an error
static IAsyncPolicy<HttpResponseMessage> GetPolicy()
	=> HttpPolicyExtensions
		.HandleTransientHttpError()
		.OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
		.WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(3));
