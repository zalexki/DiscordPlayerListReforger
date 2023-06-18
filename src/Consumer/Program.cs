using System.Globalization;
using Discord.WebSocket;
using DiscordPlayerListConsumer.Services;
using DiscordPlayerListConsumer.Services.BackgroundServices;
using DiscordPlayerListConsumer.Services.Helpers;
using DiscordPlayerListShared.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Serilog.Debugging.SelfLog.Enable(Console.WriteLine);
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services
            .AddSingleton<MemoryStorage>()
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton<RabbitConnection>()

            .AddScoped<DiscordHelper>()
            
            .AddHostedService<RabbitConsumer>()
            .AddHostedService<DplBackgroundService>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .Build();

host.UseEnvironment();
host.Run();
