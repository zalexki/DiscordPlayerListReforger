using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DiscordPlayerList.Services.BackgroundService;

public class DplBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly ILogger<DplBackgroundService> _logger;
    private readonly DiscordHelper _discord;
    private readonly MemoryStorage _memoryStorage;

    public DplBackgroundService(ILogger<DplBackgroundService> logger, DiscordHelper discord, MemoryStorage memoryStorage)
    {
        _logger = logger;
        _discord = discord;
        _memoryStorage = memoryStorage;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DoWork();

        using PeriodicTimer timer = new(TimeSpan.FromMinutes(3));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                DoWork();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");
        }
    }
    
    private async void DoWork()
    {
        var list = _memoryStorage.DiscordChannels
            .Where(x => x.IsUp && x.LastUpdate < DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5)));

        foreach (var discordChannelTracked in list)
        {
            try
            {
                var sendOffSuccess = await _discord.SendServerOffFromTrackedChannels(discordChannelTracked);
                if (sendOffSuccess)
                {
                    discordChannelTracked.IsUp = false;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to sendServerOff for {@DiscordChannelTracked}", discordChannelTracked);
            }
            
            await Task.Delay(100);
        }
    }
}