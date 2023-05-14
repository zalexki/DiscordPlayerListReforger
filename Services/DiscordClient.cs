using Discord;
using Discord.WebSocket;
using discordPlayerList.Models.Request;

namespace discordPlayerList.Services;

public class DiscordClient
{
    private readonly ILogger<DiscordClient> _logger;
    private readonly DiscordSocketClient _client;
    
    public DiscordClient(ILogger<DiscordClient> logger)
    {
        _logger = logger;
        _client = new DiscordSocketClient();
        _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
        _client.StartAsync();
    }

    public async Task<bool> SendMessageFromGameData(ServerGameData data)
    {
        try
        {
            var channel = await _client.GetChannelAsync(data.DiscordChannelId);
            var chanText = channel as ITextChannel;
            if (chanText is null)
            {
                _logger.LogError("failed to cast to ITextChannel");
                
                return false;
            }

            var userBotId = _client.CurrentUser.Id;
            var messages = await chanText.GetMessagesAsync(10).FlattenAsync();
            var embed = new EmbedBuilder();

            embed
                .AddField("Active players", RabbitToDiscordConverter.GetPlayerList(data))
                .AddField("Mission data", RabbitToDiscordConverter.GetMissionData(data))
                // .WithFooter(footer => footer.Text = "Powered by Sen")
                .WithColor(Color.DarkTeal)
                .WithTitle($"-- {data.DiscordChannelName} -- [{data.PlayerCount}/{data.MaxPlayerCount}]")
                .WithCurrentTimestamp();

            var botMessages = messages.Where(x => x.Author.Id == userBotId).ToList();
            if (botMessages.Any())
            {
                var first = botMessages.First();
                
                foreach (var message in botMessages.Where(message => first.Id != message.Id))
                {
                    await chanText.DeleteMessageAsync(message.Id);
                }
                
                await chanText.ModifyMessageAsync(first.Id, func: x => x.Embed = embed.Build());
            }
            else
            {
                await chanText.SendMessageAsync(embed: embed.Build());
            }

        }
        catch (Exception e)
        {
            _logger.LogError(e, "failed to send discord msg");
            return false;
        }
        
        return true;
    }
}