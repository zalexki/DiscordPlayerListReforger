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
        // wait for connection to be done
        var i = 1;
        while (_client.LoginState != LoginState.LoggedIn)
        {
            if (_client.LoginState == LoginState.LoggedOut)
            {
                await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
                await _client.StartAsync();
            }
            Thread.Sleep(1000 * i);
            i++;

            if (i > 20)
            {
                throw new Exception("could not connect to discord api");
            }
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

            var channelName = $"{data.DiscordChannelName.Trim()}〔{data.ServerInfo.PlayerCount}∕{data.ServerInfo.MaxPlayerCount}〕"; 
            await chanText.ModifyAsync(props => { props.Name = channelName;});

            while (_client.CurrentUser is null)
            {
                Thread.Sleep(100);
            }
            var userBotId = _client.CurrentUser.Id;
            var messages = await chanText.GetMessagesAsync(10).FlattenAsync();
            var embed = new EmbedBuilder();

            // TODO: find a way for a better presentation
            embed
                .WithTitle($"-- {data.DiscordMessageTitle} -- [{data.ServerInfo.PlayerCount}/{data.ServerInfo.MaxPlayerCount}]")

                .AddField("▬▬▬▬▬▬▬▬▬▬ Server Information ▬▬▬▬▬▬▬▬▬▬", "╰┈➤")
                .AddField("Active players", RabbitToDiscordConverter.GetPlayerList(data), true)
                .AddField("Server", RabbitToDiscordConverter.GetServerData(data.ServerInfo), true)

                .AddField("▬▬▬▬▬▬▬▬▬▬ Mission Information ▬▬▬▬▬▬▬▬▬▬", "╰┈➤")
                .AddField("Mission", data.ServerInfo.MissionName, true)
                .AddField("Time", data.ServerInfo.TimeInGame, true)
                .AddField("Wind", RabbitToDiscordConverter.GetWindData(data.ServerInfo), true)

                .WithFooter(footer => footer.Text = $"Updated at {DateTime.UtcNow:M/d/yy HH:mm:ss} UTC")
                .WithColor(Color.DarkTeal);
                // .WithCurrentTimestamp();
            
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