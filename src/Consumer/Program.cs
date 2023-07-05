using System;
using System.Globalization;
using Discord.WebSocket;
using DiscordPlayerListConsumer.Services;
using DiscordPlayerListConsumer.Services.BackgroundServices;
using DiscordPlayerListConsumer.Services.Helpers;
using DiscordPlayerListShared.Extensions;
using DiscordPlayerListShared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

var host = Host.CreateDefaultBuilder(args)
    .UseEnvironmentFromDotEnv()
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
    .UseSerilog((hostingContext, services, loggerConfiguration) => loggerConfiguration
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.NewRelicLogs(
            licenseKey: Environment.GetEnvironmentVariable("NEW_RELIC_KEY"),
            endpointUrl: "https://log-api.eu.newrelic.com/log/v1",
            applicationName: "DPL-Consumer")
    )
    .Build();

host.Run();
