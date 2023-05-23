using System.Globalization;
using DiscordPlayerList.Extensions;
using DiscordPlayerList.Services;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);
builder.UseEnvironment();
builder.WebHost.UseUrls("http://0.0.0.0:5000");

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddSingleton<DiscordChannelList>();
        // .AddSingleton<DiscordSocketConfig>()
        // .AddSingleton<DiscordSocketClient>()
builder.Services.AddSingleton<DiscordClient>();
builder.Services.AddSingleton<RabbitConnectionConsumer>();
builder.Services.AddSingleton<RabbitConnectionPublisher>();

builder.Services.AddHostedService<RabbitConsumer>();
builder.Services.AddHostedService<DplBackgroundService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Config logging
builder.Logging.ClearProviders().AddConsole();

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

app.Run();
