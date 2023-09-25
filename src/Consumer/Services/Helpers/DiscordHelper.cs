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

namespace DiscordPlayerListConsumer.Services.Helpers;

public class DiscordHelper
{
    private readonly ILogger<DiscordHelper> _logger;
    private readonly DiscordSocketClient _client;
    private readonly MemoryStorage _memoryStorage;
    private readonly RedisStorage _redisStorage;
    private int retrySendName;
    private int retrySendMessage;
    public const int DISCORD_FIELD_MAX_LENGTH = 1024;


    public DiscordHelper(ILogger<DiscordHelper> logger, DiscordSocketClient client, MemoryStorage memoryStorage, 
        RedisStorage redisStorage)
    {
        _logger = logger;
        _client = client;
        _memoryStorage = memoryStorage;
        _redisStorage = redisStorage;
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
            var existingChannel =
                _memoryStorage.DiscordChannels.SingleOrDefault(x => x.ChannelId == data.DiscordChannelId);
            if (existingChannel != null && existingChannel.ComputedChannelName != channelName)
            {
                await SendChannelName(chanText, data, channelName);
                _logger.LogInformation("try update channel name from {ComputedChannelName} to {ChannelName}",
                    existingChannel.ComputedChannelName, channelName);
                existingChannel.ComputedChannelName = channelName;
            }

            // update message
            var missionName =
                RabbitToDiscordConverter.ResolveShittyBohemiaMissionName(data.ServerInfo?.MissionName ?? string.Empty);
            var players = RabbitToDiscordConverter.GetPlayerList(data);
            //var playerExtraPlatformFaction = RabbitToDiscordConverter.GetPlayerExtrasPlatformFaction(data, players.count);
            var playerExtraKillDeath = RabbitToDiscordConverter.GetPlayerExtrasKillDeath(data, players.count);
            var playerFriendlyKills = RabbitToDiscordConverter.GetPlayerFriendlyKills(data, players.count);
            var server = RabbitToDiscordConverter.GetServerData(data.ServerInfo, _logger);
            var wind = RabbitToDiscordConverter.GetWindData(data.ServerInfo);

            var embed = new EmbedBuilder();
            embed
                .WithTitle($"-- {data.DiscordMessageTitle} -- [{playerCount}/{data.ServerInfo?.MaxPlayerCount}]")

                // empty line
                .AddField("** **", "** **")

                .AddField("▬▬▬▬▬▬▬▬▬▬ Server Information ▬▬▬▬▬▬▬▬▬▬", "╰┈➤")
                .AddField("Active players name", players.data, true)
                // .AddField("Faction ", playerExtraPlatformFaction, true)
                .AddField("K | D ", playerExtraKillDeath, true)
                .AddField("FP Kills | FA Kills", playerFriendlyKills, true)
                .AddField("** **", "** **")
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

            var memChan = _memoryStorage.DiscordChannels.FirstOrDefault(x => x.ChannelId == data.DiscordChannelId);
            if (memChan is not null && memChan.FirstMessageId != 0L)
            {
                await SendMessage(chanText, memChan, embed);
            }
            else
            {
                await GetBotUserId();

                _logger.LogInformation(
                    "perfProfile: _client.CurrentUser is null done for channelId {ChanId} in {Time} ms",
                    data.DiscordChannelId, swCurrent.ElapsedMilliseconds);
                swCurrent.Restart();

                var messages = await chanText.GetMessagesAsync(10).FlattenAsync();
                var botMessages = messages.Where(x => x.Author.Id == _memoryStorage.BotUserId).ToList();

                _logger.LogInformation("perfProfile: retrieve first msg done for channelId {ChanId} in {Time} ms",
                    data.DiscordChannelId, swCurrent.ElapsedMilliseconds);
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
                    _ = Task.Run(() => chanText.SendMessageAsync(embed: embed.Build(),
                        options: new RequestOptions() {Timeout = 25000, RetryMode = RetryMode.AlwaysRetry}));
                }
            }

            _logger.LogInformation("perfProfile: TOTAL channelId {ChanId} in {Time} ms", data.DiscordChannelId,
                sw.ElapsedMilliseconds);
            sw.Stop();
            swCurrent.Stop();
        }
        catch (HttpException e)
        {
            if (e.DiscordCode == DiscordErrorCode.MissingPermissions)
            {
                _logger.LogError(e, "failed to send discord msg, no perms");
                _redisStorage.SaveIntoRedisMissingPerms(data.DiscordChannelId);    
            } 

            _logger.LogError(e, "HttpException failed to send discord msg");
            return false;
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
        if (retrySendMessage > 5)
        {
            _logger.LogWarning("stop retrySendMessage for chan {Name}", memChan.ChannelName);
            retrySendMessage = 0;
            return;
        }
        
        await Task.Delay(_memoryStorage.waitBeforeSendChannelMessage);
        _logger.LogWarning("SendMessage waited waitBeforeSendChannelMessage for chan {Name} {Time}", 
            memChan.ChannelName, 
            _memoryStorage.waitBeforeSendChannelMessage);

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
            _logger.LogInformation("perfProfile: {Try} send modify msg done for channelId {ChanId} in {Time}ms",
                retrySendMessage, memChan.ChannelId, timer.ElapsedMilliseconds);
            
        }
        catch (RateLimitedException e)
        {
            _logger.LogWarning("RateLimitedException SendMessage for chan {Name} timeout is {Timeout}", memChan.ChannelName, e.Request.TimeoutAt);
            if (_memoryStorage.waitBeforeSendChannelMessage.TotalMilliseconds == 0 || retrySendMessage > 1)
            {
                _memoryStorage.waitBeforeSendChannelMessage = e.Request.TimeoutAt - DateTime.UtcNow ?? new TimeSpan();
                _logger.LogWarning("new waitBeforeSendChannelMessage is {Time}", _memoryStorage.waitBeforeSendChannelMessage);
            }
            
            await Task.Delay(_memoryStorage.waitBeforeSendChannelMessage.Add(TimeSpan.FromSeconds(6)));
            await SendMessage(chanText, memChan, embed);
            return;
        }
        catch (TimeoutException e)
        {
            _logger.LogError("TimeoutException to modify msg for channel {ChanName} {ChanId}", memChan.ChannelName, memChan.ChannelId);
        }
        catch (Exception e) 
        {
            _logger.LogError(e, "failed to modify msg for channel {ChanName} {ChanId}", memChan.ChannelName, memChan.ChannelId);
            memChan.FirstMessageId = 0L;
            _redisStorage.SaveIntoRedis(memChan);
        }

        _memoryStorage.waitBeforeSendChannelMessage = new TimeSpan();
        retrySendMessage = 0;
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
        if (retrySendName > 5)
        {
            _logger.LogWarning("stop retrySendName for chan {Name}", channelName);
            var memChan = _memoryStorage.DiscordChannels.SingleOrDefault(x => x.ChannelName == channelName);
            if (memChan != null) memChan.ChannelName = string.Empty;
            retrySendName = 0;
            return;
        }
        
        await Task.Delay(_memoryStorage.waitBeforeSendChannelName);

        try
        {
            var timer = Stopwatch.StartNew();
            await chanText.ModifyAsync(props => { props.Name = channelName; }, 
                options: new RequestOptions
                {
                    Timeout = 5000,
                    RetryMode = RetryMode.AlwaysFail,
                    RatelimitCallback = RateLimitedCallbackModifyName
                });
            timer.Stop();
            _logger.LogInformation("perfProfile: {Try} modify channel name done for channelId {ChanId} in {Time}ms",
                retrySendMessage, channelName, timer.ElapsedMilliseconds);
        }
        catch (RateLimitedException e)
        {
            _logger.LogWarning("RateLimitedException SendMessage for chan {Name} timeout is {Timeout}", channelName, e.Request.TimeoutAt);

            if (_memoryStorage.waitBeforeSendChannelName.TotalMilliseconds == 0 || retrySendName > 1)
            {
                _memoryStorage.waitBeforeSendChannelName = e.Request.TimeoutAt - DateTime.UtcNow ?? new TimeSpan();
                _logger.LogWarning("new waitBeforeSendChannelName is {Time}", _memoryStorage.waitBeforeSendChannelName);
            }
            
            await Task.Delay(_memoryStorage.waitBeforeSendChannelName.Add(TimeSpan.FromSeconds(5)));
            await SendRateLimitSafeChannelName(chanText, channelName);
            return;
        }
        catch (TimeoutException e)
        {
            _logger.LogError("TimeoutException to modify msg for channel {ChanName}", channelName);
            var memChan = _memoryStorage.DiscordChannels.SingleOrDefault(x => x.ChannelName == channelName);
            if (memChan != null)
            {
                memChan.ChannelName = string.Empty;
                _redisStorage.SaveIntoRedis(memChan);
            }
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "failed to sendName {Name}", channelName);
            var memChan = _memoryStorage.DiscordChannels.SingleOrDefault(x => x.ChannelName == channelName);
            if (memChan != null)
            {
                memChan.ChannelName = string.Empty;
                _redisStorage.SaveIntoRedis(memChan);
            }
        }

        _memoryStorage.waitBeforeSendChannelName = new TimeSpan();
        retrySendName = 0;
        _logger.LogInformation("success update channel name {ChannelName}", channelName);

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
                var notTextChannelIds = _redisStorage.LoadFromRedisNotTextChannelIds();
                if (false == notTextChannelIds.Ids.Contains(data.ChannelId)) {
                    notTextChannelIds.Ids.Add(data.ChannelId);
                    _redisStorage.SaveIntoRedis(notTextChannelIds);
                    _logger.LogError("added data.ChannelId to obj {obj}", JsonConvert.SerializeObject(notTextChannelIds, Formatting.Indented));
                }
                
                return false;
            }

            var channelName = $"🔴|{data.ChannelName.Trim()}〔0∕0〕";
            await SendChannelName(chanText, data, channelName);
            var memChan = _memoryStorage.DiscordChannels.SingleOrDefault(x => x.ChannelId == data.ChannelId);
            if (memChan != null)
            {
                memChan.ComputedChannelName = channelName;
                _redisStorage.SaveIntoRedis(memChan);
            }

            var embed = new EmbedBuilder();
            embed.AddField("▬▬▬▬▬▬▬▬▬▬ Server Information ▬▬▬▬▬▬▬▬▬▬", "server offline")
                .WithFooter(footer => footer.Text = "🙃")
                .WithColor(Color.DarkTeal)
                .WithCurrentTimestamp();

            if (memChan is not null && memChan.FirstMessageId != 0L)
            {
                await SendMessage(chanText, memChan, embed);
            }
            sw.Stop();
            _logger.LogInformation("finished to send server off discord msg in {Time}ms", sw.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "failed to send server off discord msg {Chan}", data.ChannelName);
            return false;
        }
        
        
        return true;
    }

    private async Task GetBotUserId()
    {
        if (_memoryStorage.BotUserId == 0L)
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

            _memoryStorage.BotUserId = _client.CurrentUser.Id;
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

    private static string HandleMaxLength(string message)
    {
        if (message.Length > 1024) {
            return message.Substring(0, 1024);
        }

        return message;
    }

    private async Task<ITextChannel> GetTextChannel(ServerGameData data)
    {
        var memoryChan = _memoryStorage.DiscordTextChannels.FirstOrDefault(x => x.Key == data.DiscordChannelId);
        if (memoryChan.Value != null)
        {
            return memoryChan.Value;
        }

        if (_memoryStorage.waitBeforeGetChannel.TotalMilliseconds > 0) await Task.Delay(_memoryStorage.waitBeforeGetChannel);
        
        var channel = await _client.GetChannelAsync(data.DiscordChannelId, 
            options: new RequestOptions
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
            var notTextChannelIds = _redisStorage.LoadFromRedisNotTextChannelIds();
            if (notTextChannelIds.Ids is null)
            {
                notTextChannelIds.Ids = new List<ulong>{data.DiscordChannelId};
                _redisStorage.SaveIntoRedis(notTextChannelIds);
            } 
            else 
            {
                if (false == notTextChannelIds.Ids.Contains(data.DiscordChannelId)) {
                    _redisStorage.SaveIntoRedis(notTextChannelIds);
                }
            }

            _logger.LogError("failed to cast to ITextChannel {Id}", data.DiscordChannelId);
                
            return null;
        }
           
        _memoryStorage.DiscordTextChannels.Add(data.DiscordChannelId, chanText);

        return chanText;
    }
    
    private async Task RateLimitedCallbackGetChannel(IRateLimitInfo rateLimitInfo)
    {
        _logger.LogWarning("rate limited GetChannel {infos}", JsonConvert.SerializeObject(rateLimitInfo, Formatting.Indented));
        _memoryStorage.waitBeforeGetChannel = rateLimitInfo.ResetAfter ?? new TimeSpan();
    }
    private async Task RateLimitedCallbackModifyMessage(IRateLimitInfo rateLimitInfo)
    {
        _logger.LogWarning("rate limited Message {infos}", JsonConvert.SerializeObject(rateLimitInfo, Formatting.Indented));
        if (rateLimitInfo.Remaining == 1)
        {
            _memoryStorage.waitBeforeSendChannelMessage = rateLimitInfo.ResetAfter ?? new TimeSpan();
        }
    }
    private async Task RateLimitedCallbackModifyName(IRateLimitInfo rateLimitInfo)
    {
        _logger.LogWarning("rate limited Name {infos}", JsonConvert.SerializeObject(rateLimitInfo, Formatting.Indented));
        _memoryStorage.waitBeforeSendChannelName = rateLimitInfo.ResetAfter ?? new TimeSpan();
    }
}
