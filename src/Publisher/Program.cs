using System;
using System.Globalization;
using DiscordPlayerListShared.Extensions;
using DiscordPlayerListShared.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args)
    .UseEnvironmentFromDotEnv();

builder.WebHost.UseUrls("http://0.0.0.0:5000");

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddSingleton<RabbitConnection>();
builder.Services.Configure<FormOptions>(x => { x.KeyLengthLimit = int.MaxValue; });

// Config logging
builder.Host.UseSerilog((hostingContext, services, loggerConfiguration) => loggerConfiguration
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.NewRelicLogs(
        licenseKey: Environment.GetEnvironmentVariable("NEW_RELIC_KEY"),
        endpointUrl: "https://log-api.eu.newrelic.com/log/v1",
        applicationName: "DPL-Publisher"));


var app = builder.Build();
// app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
