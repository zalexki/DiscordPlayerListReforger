using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using DiscordPlayerList.Models;
using DiscordPlayerList.Models.Request;
using DiscordPlayerList.Services.Converter;
using Microsoft.Extensions.Logging;

namespace DiscordPlayerList.Services;

public class DiscordHelper
{
    private readonly ILogger<DiscordHelper> _logger;
    private readonly DiscordRestClient _client;
    
    public DiscordHelper(ILogger<DiscordHelper> logger)
    {
        _logger = logger;
        _client = new DiscordRestClient();
    }

    public async Task<bool> SendServerOffFromTrackedChannels(DiscordChannelTracked data)
    {
        await WaitForConnection();
        
        try
        {
            var channel = await _client.GetChannelAsync(data.ChannelId);
            var chanText = channel as ITextChannel;
            if (chanText is null)
            {
                _logger.LogError("failed to cast to ITextChannel");
                
                return false;
            }

            var channelName = $"🔴{data.ChannelName.Trim()}〔0∕0〕"; 
            await chanText.ModifyAsync(props => { props.Name = channelName;});
        }
        catch (Exception e)
        {
            _logger.LogError(e, "failed to send discord msg");
            return false;
        }
        
        return true;
    }

    public async Task<bool> SendMessageFromGameData(ServerGameData data)
    {
        await WaitForConnection();
        
        try
        {
            var channel = await _client.GetChannelAsync(data.DiscordChannelId);
            var chanText = channel as ITextChannel;
            if (chanText is null)
            {
                _logger.LogError("failed to cast to ITextChannel");
                
                return false;
            }

            var channelName = $"🟢{data.DiscordChannelName.Trim()}〔{data.ServerInfo.PlayerCount}∕{data.ServerInfo?.MaxPlayerCount}〕"; 
            await chanText.ModifyAsync(props => { props.Name = channelName;});

            while (_client.CurrentUser is null)
            {
                await Task.Delay(100);
            }
            var userBotId = _client.CurrentUser.Id;
            var missionName = RabbitToDiscordConverter.ResolveShittyBohemiaMissionName(data.ServerInfo?.MissionName ?? string.Empty);
            var players = RabbitToDiscordConverter.GetPlayerList(data);
            var server = RabbitToDiscordConverter.GetServerData(data.ServerInfo);
            var wind = RabbitToDiscordConverter.GetWindData(data.ServerInfo);

            var embed = new EmbedBuilder();

            embed
                .WithTitle($"-- {data.DiscordMessageTitle} -- [{data.ServerInfo.PlayerCount}/{data.ServerInfo.MaxPlayerCount}]")
                
                // empty line
                .AddField("** **", "** **")
                
                .AddField("▬▬▬▬▬▬▬▬▬▬ Server Information ▬▬▬▬▬▬▬▬▬▬", "╰┈➤")
                .AddField("Active players", players, true)
                .AddField("Server", server, true)
                
                // empty line
                .AddField("** **", "** **")
                
                .AddField("▬▬▬▬▬▬▬▬▬▬ Mission Information ▬▬▬▬▬▬▬▬▬▬", "╰┈➤")
                .AddField("Mission", missionName, true)
                .AddField("Time", data.ServerInfo.TimeInGame, true)
                .AddField("Wind", wind, true)
                
                // empty line
                .AddField("** **", "** **")

                .WithFooter(footer => footer.Text = "☺")
                .WithColor(Color.DarkTeal)
                .WithCurrentTimestamp();

            var messages = await chanText.GetMessagesAsync(10).FlattenAsync();
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
    
    private async Task WaitForConnection()
    {
        var i = 1;
        while (_client.LoginState != LoginState.LoggedIn)
        {
            if (_client.LoginState == LoginState.LoggedOut)
            {
                await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
                // await _client.StartAsync();
            }
            await Task.Delay(100 * i);
            i++;

            if (i > 20)
            {
                throw new Exception("could not connect to discord api");
            }
        }
    }
}