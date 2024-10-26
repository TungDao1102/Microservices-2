using AuctionService.Consumers;
using AuctionService.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<AuctionDbContext>(opt =>
{
	opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddMassTransit(options =>
{
	options.AddEntityFrameworkOutbox<AuctionDbContext>(x =>
	{
		x.QueryDelay = TimeSpan.FromSeconds(10);
		x.UsePostgres();
		x.UseBusOutbox();
	});

	options.AddConsumersFromNamespaceContaining<AuctionCreatedFaultConsumer>();
	//options.SetKebabCaseEndpointNameFormatter();
	options.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("auction", false));
	options.UsingRabbitMq((context, config) =>
	{
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

app.InitDb();

app.MapControllers();

app.Run();
