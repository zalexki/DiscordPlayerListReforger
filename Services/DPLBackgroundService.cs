namespace discordPlayerList.Services;

public class DplBackgroundService : BackgroundService
{
    private readonly ILogger<DplBackgroundService> _logger;
    private readonly DiscordClient _discord;
    private readonly DiscordChannelList _discordChannelList;

    public DplBackgroundService(ILogger<DplBackgroundService> logger, DiscordClient discord, DiscordChannelList discordChannelList)
    {
        _logger = logger;
        _discord = discord;
        _discordChannelList = discordChannelList;
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
        var list = _discordChannelList.DiscordChannels
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
            
            Thread.Sleep(1000);
        }
    }
}