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
using Discord.Net;
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
    private int retrySendName;
    private int retrySendMessage;


    public DiscordHelper(ILogger<DiscordHelper> logger, DiscordSocketClient client, MemoryStorage listOfChannels, IConnectionMultiplexer multiplexerRedis, DPLJsonConverter jsonConverter)
    {
        _logger = logger;
        _client = client;
        _listOfChannels = listOfChannels;
        _multiplexerRedis = multiplexerRedis;
        _jsonConverter = jsonConverter;
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
            var chanText = await GetTextChannel(data);
            if (chanText is null) return false;

            var playerCount = data.PlayerList.Count;
            
            // update channel name
            var channelName = $"🟢{data.DiscordChannelName.Trim()}〔{playerCount}∕{data.ServerInfo?.MaxPlayerCount}〕";
            var existingChannel = _listOfChannels.DiscordChannels.SingleOrDefault(x => x.ChannelId == data.DiscordChannelId);
            if (existingChannel != null && existingChannel.ComputedChannelName != channelName)
            {
                await SendChannelName(chanText, data, channelName);
                _logger.LogInformation("update channel name from {computedChannelName} to {channelName}", existingChannel.ComputedChannelName, channelName);
                existingChannel.ComputedChannelName = channelName;
            }

            // update message
            var missionName = RabbitToDiscordConverter.ResolveShittyBohemiaMissionName(data.ServerInfo?.MissionName ?? string.Empty);
            var players = RabbitToDiscordConverter.GetPlayerList(data);
            var server = RabbitToDiscordConverter.GetServerData(data.ServerInfo);
            var wind = RabbitToDiscordConverter.GetWindData(data.ServerInfo);

            var embed = new EmbedBuilder();
            embed
                .WithTitle($"-- {data.DiscordMessageTitle} -- [{playerCount}/{data.ServerInfo?.MaxPlayerCount}]")
                
                // empty line
                .AddField("** **", "** **")
                
                .AddField("▬▬▬▬▬▬▬▬▬▬ Server Information ▬▬▬▬▬▬▬▬▬▬", "╰┈➤")
                .AddField("Active players", HandleMaxLength(players), true)
                .AddField("Server", HandleMaxLength(server), true)
                
                // empty line
                .AddField("** **", "** **")
                
                .AddField("▬▬▬▬▬▬▬▬▬▬ Mission Information ▬▬▬▬▬▬▬▬▬▬", "╰┈➤")
                .AddField("Mission", HandleMaxLength(missionName), true)
                .AddField("Time", HandleMaxLength(data.ServerInfo?.TimeInGame), true)
                .AddField("Wind", HandleMaxLength(wind), true)
                
                // empty line
                .AddField("** **", "** **")

                .WithFooter(footer => footer.Text = "🙂")
                .WithColor(Color.DarkTeal)
                .WithCurrentTimestamp();

            var memChan = _listOfChannels.DiscordChannels.FirstOrDefault(x => x.ChannelId == data.DiscordChannelId);
            if (memChan is not null && memChan.FirstMessageId != 0L)
            {
                await SendMessage(chanText, memChan, embed);
            } 
            else
            {
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

                    await SendMessage(chanText, memChan, embed);
                }
                else
                {
                    _ = Task.Run(() => chanText.SendMessageAsync(embed: embed.Build(), options: new RequestOptions() { Timeout = 25000, RetryMode = RetryMode.AlwaysRetry }));
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
    
    private async Task SendMessage(ITextChannel chanText, DiscordChannelTracked memChan, EmbedBuilder embed)
    {
        retrySendMessage++;
        if (retrySendMessage > 10)
        {
            _logger.LogWarning("stop retrySendMessage for chan {Name}", memChan.ChannelName);
            retrySendMessage = 0;
            return;
        }
        
        if (_listOfChannels.waitBeforeSendChannelMessage.TotalMilliseconds > 0)
        {
            await Task.Delay(_listOfChannels.waitBeforeSendChannelMessage);
            _logger.LogWarning("SendMessage waited waitBeforeSendChannelMessage for chan {Name} {Time}ms", 
                memChan.ChannelName, 
                _listOfChannels.waitBeforeSendChannelMessage.TotalMilliseconds);
        }
        
        try
        {
            var timer = Stopwatch.StartNew();
            await chanText.ModifyMessageAsync(memChan.FirstMessageId, func:
                x => x.Embed = embed.Build(),
                options: new RequestOptions()
                {
                    Timeout = 5000,
                    RetryMode = RetryMode.AlwaysFail,
                    RatelimitCallback = RateLimitedCallbackModifyMessage
                });

            timer.Stop();
            _logger.LogInformation("perfProfile: send modify msg done for channelId {ChanId} in {Time} ms",
                memChan.ChannelId, timer.ElapsedMilliseconds);
            _listOfChannels.waitBeforeSendChannelMessage = new TimeSpan();
        }
        catch (RateLimitedException e)
        {
            _logger.LogInformation("RateLimitedException SendMessage for chan {Name}", memChan.ChannelName);

            if (e.Request.TimeoutAt == null)
            {
                await Task.Delay(100);
            }

            _listOfChannels.waitBeforeSendChannelMessage = e.Request.TimeoutAt - DateTime.UtcNow ?? new TimeSpan();
            if (_listOfChannels.waitBeforeSendChannelMessage.TotalMilliseconds != 0)
            {
                await Task.Delay(_listOfChannels.waitBeforeSendChannelMessage);
                _logger.LogInformation("retried SendMessage for chan {Name} {Id} after {Time}ms", 
                    memChan.ChannelName, memChan.ChannelId, e.Request.TimeoutAt.Value.Offset);
            }
            else
            {
                await Task.Delay(100);
                _logger.LogInformation("retried SendMessage for chan {Name} {Id} after {Time}ms", 
                    memChan.ChannelName, memChan.ChannelId, 100);
            }

            await SendMessage(chanText, memChan, embed);
        }
        catch (TimeoutException e)
        {
            _logger.LogWarning("TimeoutException to modify msg for channel {ChanName} {ChanId}", memChan.ChannelName, memChan.ChannelId);
        }
        catch (Exception e) 
        {
            _logger.LogError(e, "failed to modify msg for channel {ChanName} {ChanId}", memChan.ChannelName, memChan.ChannelId);
        }
    }
    
    private async Task SendChannelName(ITextChannel chanText, ServerGameData data, string channelName)
    {
        _logger.BeginScope(new Dictionary<string, string>{ 
            ["channelId"] = data.DiscordChannelId.ToString(), 
            ["channelName"] = data.DiscordChannelName
        });

        await SendRateLimitSafeChannelName(chanText, channelName);
    }

    private async Task SendChannelName(ITextChannel chanText, DiscordChannelTracked data, string channelName)
    {
        _logger.BeginScope(new Dictionary<string, string>{ 
            ["channelId"] = data.ChannelId.ToString(), 
            ["channelName"] = data.ChannelName
        });

        await SendRateLimitSafeChannelName(chanText, channelName);
    }
    
    private async Task SendRateLimitSafeChannelName(ITextChannel chanText, string channelName)
    {
        retrySendName++;
        if (retrySendName > 10)
        {
            _logger.LogWarning("stop retrySendName for chan {Name}", channelName);

            return;
        }
        
        if (_listOfChannels.waitBeforeSendChannelName.TotalMilliseconds > 0)
        {
            await Task.Delay(_listOfChannels.waitBeforeSendChannelName);
        }
        
        try
        {
            await chanText.ModifyAsync(props => { props.Name = channelName; }, 
                options: new RequestOptions
                {
                    Timeout = 5000,
                    RetryMode = RetryMode.AlwaysFail,
                    RatelimitCallback = RateLimitedCallbackModifyName
                });
        }
        catch (RateLimitedException e)
        {
            _listOfChannels.waitBeforeSendChannelName = e.Request.TimeoutAt - DateTime.UtcNow ?? new TimeSpan();
            await Task.Delay(_listOfChannels.waitBeforeSendChannelName);
            _logger.LogInformation( "retried sendName for chan {Name} after {Time}ms", channelName, e.Request.TimeoutAt.Value.Offset);

            await SendRateLimitSafeChannelName(chanText, channelName);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "failed to sendName {Name}", channelName);
        }

        _listOfChannels.waitBeforeSendChannelName = new TimeSpan();
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
                    _logger.LogError("added data.ChannelId to obj {obj}", JsonConvert.SerializeObject(notTextChannelIds, Formatting.Indented));
                }
                
                return false;
            }

            var channelName = $"🔴|{data.ChannelName.Trim()}〔0∕0〕";
            await SendChannelName(chanText, data, channelName);
            var existingChannel = _listOfChannels.DiscordChannels.SingleOrDefault(x => x.ChannelId == data.ChannelId);
            if (existingChannel != null) existingChannel.ComputedChannelName = channelName;

            var embed = new EmbedBuilder();
            embed.AddField("▬▬▬▬▬▬▬▬▬▬ Server Information ▬▬▬▬▬▬▬▬▬▬", "server offline")
                .WithFooter(footer => footer.Text = "🙃")
                .WithColor(Color.DarkTeal)
                .WithCurrentTimestamp();

            await SendMessage(chanText, existingChannel, embed);
            sw.Stop();
            _logger.LogInformation("finished to send server off discord msg in {0} ms", sw.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "failed to send server off discord msg {chan}", data.ChannelName);
            return false;
        }
        
        return true;
    }
    
    private async Task RateLimitedCallbackGetChannel(IRateLimitInfo rateLimitInfo)
    {
        _logger.LogWarning("rate limited GetChannel {infos}", JsonConvert.SerializeObject(rateLimitInfo, Formatting.Indented));
    }
    private async Task RateLimitedCallbackModifyMessage(IRateLimitInfo rateLimitInfo)
    {
        _logger.LogWarning("rate limited Message {infos}", JsonConvert.SerializeObject(rateLimitInfo, Formatting.Indented));
    }
    private async Task RateLimitedCallbackModifyName(IRateLimitInfo rateLimitInfo)
    {
        _logger.LogWarning("rate limited Name {infos}", JsonConvert.SerializeObject(rateLimitInfo, Formatting.Indented));
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
            var data = redisDb.StringGet(NotTextChannelIds.REDIS_KEY);
            if (false == data.IsNull)
            {
                return _jsonConverter.ToObject<NotTextChannelIds>(data.ToString());
            }
        }  
        catch (Exception e) 
        {
            _logger.LogError(e, "LoadFromRedisNotTextChannelIds error");
        }

        return new NotTextChannelIds(){ Ids = new List<ulong>() };
    }

    private async Task<ITextChannel> GetTextChannel(ServerGameData data)
    {
        var memoryChan = _listOfChannels.DiscordChannels.SingleOrDefault(x => x.ChannelId == data.DiscordChannelId);

        // if (memoryChan is {chanText: not null})
        // {
        //     return memoryChan.chanText;
        // }
        
        var channel = await _client.GetChannelAsync(data.DiscordChannelId, 
            options: new RequestOptions()
            {
                RetryMode = RetryMode.AlwaysRetry,
                RatelimitCallback = RateLimitedCallbackGetChannel
            });
        
        if (channel is null)
        {
            _logger.LogError("failed to retrieve channel for {Id}", data.DiscordChannelId);
            return null;
        }
            
        var chanText = channel as ITextChannel;
        if (chanText is null)
        {
            // save id into redis to allow publisher to respond bad request 
            var notTextChannelIds = LoadFromRedisNotTextChannelIds();
            if (notTextChannelIds.Ids is null)
            {
                notTextChannelIds.Ids = new List<ulong>{data.DiscordChannelId};
                SaveIntoRedis(notTextChannelIds);
            } 
            else 
            {
                if (false == notTextChannelIds.Ids.Contains(data.DiscordChannelId)) {
                    SaveIntoRedis(notTextChannelIds);
                }
            }

            _logger.LogError("failed to cast to ITextChannel {Id}", data.DiscordChannelId);
                
            return null;
        }
           
        // memoryChan.chanText = chanText;

        return chanText;
    }
}
