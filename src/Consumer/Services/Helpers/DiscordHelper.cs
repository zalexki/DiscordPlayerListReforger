using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using DiscordPlayerListShared.Models.Request;
using DiscordPlayerListConsumer.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using StackExchange.Redis;
using DiscordPlayerListConsumer.Models.Redis;
using DiscordPlayerListShared.Converter;

namespace DiscordPlayerListConsumer.Services.Helpers;

public class DiscordHelper
{
    private readonly ILogger<DiscordHelper> _logger;
    private readonly DiscordSocketClient _client;
    private readonly MemoryStorage _listOfChannels;
    private readonly IConnectionMultiplexer _multiplexerRedis;
    private readonly DPLJsonConverter _jsonConverter;


    public DiscordHelper(ILogger<DiscordHelper> logger, DiscordSocketClient client, MemoryStorage listOfChannels, IConnectionMultiplexer multiplexerRedis, DPLJsonConverter jsonConverter)
    {
        _logger = logger;
        _client = client;
        _listOfChannels = listOfChannels;
        _multiplexerRedis = multiplexerRedis;
        _jsonConverter = jsonConverter;
    }

    public async Task<bool> SendServerOffFromTrackedChannels(DiscordChannelTracked data)
    {
        await WaitForConnection();
         _logger.BeginScope(new Dictionary<string, string>{ 
            ["channelId"] = data.ChannelId.ToString(), 
            ["channelName"] = data.ChannelName
        });
        var sw = Stopwatch.StartNew();
        
        try
        {
            var channel = await _client.GetChannelAsync(data.ChannelId);
            var chanText = channel as ITextChannel;
            if (chanText is null)
            {
                var notTextChannelIds = LoadFromRedisNotTextChannelIds();
                if (false == notTextChannelIds.Ids.Contains(data.ChannelId)) {
                    notTextChannelIds.Ids.Add(data.ChannelId);
                    SaveIntoRedis(notTextChannelIds);
                }

                _logger.LogError("failed to cast to ITextChannel");
                
                return false;
            }

            var channelName = $"🔴|{data.ChannelName.Trim()}〔0∕0〕";
            await chanText.ModifyAsync(props => { props.Name = channelName; });
            var existingChannel = _listOfChannels.DiscordChannels.SingleOrDefault(x => x.ChannelId == data.ChannelId);
            existingChannel.ComputedChannelName = channelName;

            var messages = await chanText.GetMessagesAsync(10).FlattenAsync();
            var userBotId = _client.CurrentUser.Id;
            var botMessages = messages.Where(x => x.Author.Id == userBotId).ToList();
            var first = botMessages.First();

            var embed = new EmbedBuilder();
            embed.AddField("▬▬▬▬▬▬▬▬▬▬ Server Information ▬▬▬▬▬▬▬▬▬▬", "server offline")
                .WithFooter(footer => footer.Text = "🙃")
                .WithColor(Color.DarkTeal)
                .WithCurrentTimestamp();

            await chanText.ModifyMessageAsync(first.Id, func: x => x.Embed = embed.Build());
            sw.Stop();
            _logger.LogInformation("finished to send server off discord msg in {0} ms", sw.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "failed to send server off discord msg");
            return false;
        }
        
        return true;
    }
    private async Task RetyCallback(IRateLimitInfo rateLimitInfo)
    {
        _logger.LogWarning("rate limited {infos}", JsonConvert.SerializeObject(rateLimitInfo, Formatting.Indented));
    }
    public async Task<bool> SendMessageFromGameData(ServerGameData data)
    {
        _logger.BeginScope(new Dictionary<string, string>{ 
            ["channelId"] = data.DiscordChannelId.ToString(), 
            ["channelName"] = data.DiscordChannelName
        });
        
        await WaitForConnection();

        var sw = Stopwatch.StartNew();
        var swCurrent = Stopwatch.StartNew();
        
        try
        {
            var channel = await _client.GetChannelAsync(data.DiscordChannelId, options: new RequestOptions(){Timeout = 30000, RetryMode = RetryMode.AlwaysRetry, RatelimitCallback = RetyCallback});
            var chanText = channel as ITextChannel;
            if (chanText is null)
            {
                var notTextChannelIds = LoadFromRedisNotTextChannelIds();
                if (false == notTextChannelIds.Ids.Contains(data.DiscordChannelId)) {
                    notTextChannelIds.Ids.Add(data.DiscordChannelId);
                    SaveIntoRedis(notTextChannelIds);
                }
                
                _logger.LogError("failed to cast to ITextChannel {id}", data.DiscordChannelId);
                
                return false;
            }

            var playerCount = data.PlayerList.Count();
            var channelName = $"🟢{data.DiscordChannelName.Trim()}〔{playerCount}∕{data.ServerInfo?.MaxPlayerCount}〕";
            var existingChannel = _listOfChannels.DiscordChannels.SingleOrDefault(x => x.ChannelId == data.DiscordChannelId);
            
            if (existingChannel.ComputedChannelName != channelName) {
                _ = Task.Run(() => chanText.ModifyAsync(props => { props.Name = channelName; }, options: new RequestOptions() { RetryMode = RetryMode.AlwaysRetry, RatelimitCallback = RetyCallback }));
                _logger.LogInformation("update channel name from {computedChannelName} to {channelName}", existingChannel.ComputedChannelName, channelName);
                existingChannel.ComputedChannelName = channelName;
            }

            var missionName = RabbitToDiscordConverter.ResolveShittyBohemiaMissionName(data.ServerInfo?.MissionName ?? string.Empty);
            var players = RabbitToDiscordConverter.GetPlayerList(data);
            var server = RabbitToDiscordConverter.GetServerData(data.ServerInfo);
            var wind = RabbitToDiscordConverter.GetWindData(data.ServerInfo);

            var embed = new EmbedBuilder();
            embed
                .WithTitle($"-- {data.DiscordMessageTitle} -- [{playerCount}/{data.ServerInfo.MaxPlayerCount}]")
                
                // empty line
                .AddField("** **", "** **")
                
                .AddField("▬▬▬▬▬▬▬▬▬▬ Server Information ▬▬▬▬▬▬▬▬▬▬", "╰┈➤")
                .AddField("Active players", HandleMaxLength(players), true)
                .AddField("Server", HandleMaxLength(server), true)
                
                // empty line
                .AddField("** **", "** **")
                
                .AddField("▬▬▬▬▬▬▬▬▬▬ Mission Information ▬▬▬▬▬▬▬▬▬▬", "╰┈➤")
                .AddField("Mission", HandleMaxLength(missionName), true)
                .AddField("Time", HandleMaxLength(data.ServerInfo.TimeInGame), true)
                .AddField("Wind", HandleMaxLength(wind), true)
                
                // empty line
                .AddField("** **", "** **")

                .WithFooter(footer => footer.Text = "🙂")
                .WithColor(Color.DarkTeal)
                .WithCurrentTimestamp();

            var memChan = _listOfChannels.DiscordChannels.FirstOrDefault(x => x.ChannelId == data.DiscordChannelId);
            if (memChan is not null && memChan.FirstMessageId != 0L)
            {
                _ = Task.Run(() => SendMsg(chanText, memChan, embed, data));
            } else {
                await GetBotUserId();
                
                _logger.LogInformation("perfProfile: _client.CurrentUser is null done for channelId {ChanId} in {Time} ms", data.DiscordChannelId, swCurrent.ElapsedMilliseconds);
                swCurrent.Restart();
                
                var messages = await chanText.GetMessagesAsync(10).FlattenAsync();
                var botMessages = messages.Where(x => x.Author.Id == _listOfChannels.BotUserId).ToList();
                
                _logger.LogInformation("perfProfile: retrieve first msg done for channelId {ChanId} in {Time} ms", data.DiscordChannelId, swCurrent.ElapsedMilliseconds);
                swCurrent.Stop();

                if (botMessages.Any())
                {
                    var first = botMessages.First();
                    memChan.FirstMessageId = first.Id;
                    foreach (var message in botMessages.Where(message => first.Id != message.Id))
                    {
                        await chanText.DeleteMessageAsync(message.Id);
                    }
                    _ = Task.Run(() => chanText.ModifyMessageAsync(first.Id, func: x => x.Embed = embed.Build(), options: new RequestOptions() { Timeout = 25000, RatelimitCallback = RetyCallback }));
                }
                else
                {
                    _ = Task.Run(() => chanText.SendMessageAsync(embed: embed.Build(), options: new RequestOptions() { Timeout = 25000, RatelimitCallback = RetyCallback }));
                }
            }

            _logger.LogInformation("perfProfile: TOTAL channelId {ChanId} in {Time} ms", data.DiscordChannelId, sw.ElapsedMilliseconds);
            sw.Stop();
            swCurrent.Stop();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "failed to send discord msg");
            return false;
        }

        return true;
    }

    private async Task SendMsg(ITextChannel chanText, DiscordChannelTracked memChan, EmbedBuilder embed, ServerGameData data)
    {
        try 
        {
            var timer = Stopwatch.StartNew();
            await chanText.ModifyMessageAsync(memChan.FirstMessageId, func: x => x.Embed = embed.Build(), options: new RequestOptions(){Timeout = 25000, RatelimitCallback = RetyCallback});
            timer.Stop();
            _logger.LogInformation("perfProfile: send modify msg done for channelId {ChanId} in {Time} ms", data.DiscordChannelId, timer.ElapsedMilliseconds);
        }
        catch (Exception e) 
        {
            _logger.LogError(e, "failed to modify msg for channelId {ChanId}", data.DiscordChannelId);
            memChan.FirstMessageId = 0L;
        }
    }

    private async Task GetBotUserId()
    {
        if (_listOfChannels.BotUserId == 0L)
        {
            var i = 0;
            while (_client.CurrentUser is null)
            {
                await Task.Delay(100);
                i++;
                if (i > 100)
                {
                    throw new Exception("failed to find bot user ID");
                }
            }

            _listOfChannels.BotUserId = _client.CurrentUser.Id;
        }
    }
    
    private async Task WaitForConnection()
    {
        var i = 1;
        while (_client.LoginState != LoginState.LoggedIn)
        {
            if (_client.LoginState == LoginState.LoggedOut)
            {
                await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
                await _client.StartAsync();
            }
            await Task.Delay(100 * i);
            i++;

            if (i > 20)
            {
                throw new Exception("could not connect to discord api");
            }
        }
    }

    private string HandleMaxLength(string message)
    {
        if (message.Length > 1024) {
            return message.Substring(0, 1024);
        }

        return message;
    }

    private void SaveIntoRedis(NotTextChannelIds obj)
    {
        var redisDb = _multiplexerRedis.GetDatabase(NotTextChannelIds.REDIS_DB);
        var json = _jsonConverter.FromObject(obj);
        redisDb.StringSet(NotTextChannelIds.REDIS_KEY, json, TimeSpan.FromDays(7));
    }

    private NotTextChannelIds LoadFromRedisNotTextChannelIds()
    {
        try
        {
            var redisDb = _multiplexerRedis.GetDatabase(NotTextChannelIds.REDIS_DB);
            var data = redisDb.StringGet(NotTextChannelIds.REDIS_KEY).ToString();

            if (data != string.Empty) {
                return _jsonConverter.ToObject<NotTextChannelIds>(data);
            }
        }  
        catch (Exception e) 
        {
            _logger.LogError(e, "LoadFromRedisNotTextChannelIds error");
        }

        return new NotTextChannelIds();
    }
}
