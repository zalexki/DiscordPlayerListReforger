using System;
using System.Globalization;
using Discord.WebSocket;
using DiscordPlayerListConsumer.Services;
using DiscordPlayerListConsumer.Services.BackgroundServices;
using DiscordPlayerListConsumer.Services.Helpers;
using DiscordPlayerListShared.Converter;
using DiscordPlayerListShared.Extensions;
using DiscordPlayerListShared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewRelic.LogEnrichers.Serilog;
using Serilog;
using StackExchange.Redis;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

var host = Host.CreateDefaultBuilder(args)
    .UseEnvironmentFromDotEnv()
    .ConfigureServices(services =>
    {
        var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? 
                           throw new Exception("missing REDIS_HOST env");
        var redisPass = Environment.GetEnvironmentVariable("REDIS_PASS") ?? 
                        throw new Exception("missing REDIS_PASS env");

        services
            .AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(
                $"{redisHost}:6379,name=dpl-consumer,password={redisPass},allowAdmin=true"))
            .AddSingleton<RabbitConnection>()
            .AddSingleton<DiscordSocketClient>()
            
            .AddSingleton<DPLJsonConverter>()
            .AddSingleton<RedisStorage>()
            .AddSingleton<MemoryStorage>()

            .AddTransient<DiscordHelper>()
            
            .AddHostedService<RabbitConsumer>()
            .AddHostedService<DplBackgroundService>();
    })
    .UseSerilog((hostingContext, services, loggerConfiguration) => loggerConfiguration
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .Enrich.WithNewRelicLogsInContext()
        .WriteTo.NewRelicLogs(
            licenseKey: Environment.GetEnvironmentVariable("NEW_RELIC_KEY"),
            endpointUrl: "https://log-api.eu.newrelic.com/log/v1",
            applicationName: "DPL-Consumer")
    )
    .Build();

// TableToImageHelper.GenerateImageTable();

// host.Run();

