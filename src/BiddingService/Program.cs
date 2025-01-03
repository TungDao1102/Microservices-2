using BiddingService.Consumers;
using MassTransit;
using MongoDB.Driver;
using MongoDB.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(options =>
{
    options.AddConsumersFromNamespaceContaining<AuctionCreatedConsumer>();
    options.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("bids", false));
    options.UsingRabbitMq((context, config) =>
    {
        config.Host(builder.Configuration["RabbitMq:Host"], "/", h =>
        {
            h.Username(builder.Configuration.GetValue("RabbitMQ:Username", "guest")!);
            h.Password(builder.Configuration.GetValue("RabbitMQ:Password", "guest")!);
        });

        config.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

await DB.InitAsync("BiddingDb",
    MongoClientSettings.FromConnectionString(app.Configuration.GetConnectionString("MongoDbConnection")));

app.Run();
