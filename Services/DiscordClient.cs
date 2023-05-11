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
            var messages = chanText.GetMessagesAsync();
            var firstPage = await messages.FirstAsync();
            var embed = new EmbedBuilder();

            embed
                .AddField("Active players", RabbitToDiscordConverter.GetPlayerList(data))
                .WithFooter(footer => footer.Text = "Powered by FFR team")
                .WithColor(Color.DarkTeal)
                .WithTitle($"-- ServerName -- [{data.PlayerCount}/{data.MaxPlayerCount}]")
                .WithCurrentTimestamp();

            if (firstPage is not null)
            {
                var botMessages = firstPage.Where(x => x.Author.Id == userBotId);
                var first = botMessages.First();
                
                foreach (var message in botMessages)
                {
                    if (first.Id == message.Id)
                    {
                        continue;
                    }
                
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
            _logger.LogError(e, "failed to send msg");
            return false;
        }
        
        return true;
    }
}