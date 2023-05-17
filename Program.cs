using discordPlayerList.Extensions;
using discordPlayerList.Services;

var builder = WebApplication.CreateBuilder(args);
builder.UseEnvironment();
builder.WebHost.UseUrls("http://0.0.0.0:5000");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSingleton<DiscordClient>();
builder.Services.AddSingleton<RabbitConnection>();
builder.Services.AddHostedService<RabbitConsumer>();

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
