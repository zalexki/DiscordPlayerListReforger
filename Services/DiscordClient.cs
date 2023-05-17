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
    }

    public async Task<bool> SendMessageFromGameData(ServerGameData data)
    {
        if (_client.LoginState == LoginState.LoggedOut)
        {
            await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
            await _client.StartAsync();
        }

        try
        {
            var channel = await _client.GetChannelAsync((ulong) data.DiscordChannelId);
            var chanText = channel as ITextChannel;
            if (chanText is null)
            {
                _logger.LogError("failed to cast to ITextChannel");
                
                return false;
            }

            var channelName = $"{data.DiscordChannelName.Trim()}〔{data.PlayerCount}᲼᲼∕᲼᲼{data.MaxPlayerCount}〕"; 
            await chanText.ModifyAsync(props => { props.Name = channelName;});
            
            var userBotId = _client.CurrentUser.Id;
            var messages = await chanText.GetMessagesAsync(10).FlattenAsync();
            var embed = new EmbedBuilder();

            // TODO: find a way for a better presentation
            embed
                .WithTitle($"-- {data.DiscordChannelName} -- [{data.PlayerCount}/{data.MaxPlayerCount}]")
                
                .AddField("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬", "᲼᲼")
                .AddField("Active players", RabbitToDiscordConverter.GetPlayerList(data), true)
                .AddField("Server IP / Port", "213.202.254.147:2002", true)
                .AddField("Runtime", "fetch runtime somewhere", true)
                
                .AddField("▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬", "᲼᲼")
                .AddField("Mission data", RabbitToDiscordConverter.GetMissionData(data), true)
                
                .WithFooter(footer => footer.Text = $"Updated at {DateTime.Now:M/d/yy HH:mm:ss}")
                .WithColor(Color.DarkTeal)
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