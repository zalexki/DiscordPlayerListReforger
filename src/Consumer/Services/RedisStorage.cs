using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Discord;
using DiscordPlayerListConsumer.Models;
using DiscordPlayerListConsumer.Models.Redis;
using DiscordPlayerListConsumer.Services.BackgroundServices;
using DiscordPlayerListShared.Converter;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace DiscordPlayerListConsumer.Services;

public class RedisStorage
{
    private const int REDIS_DB = 1;
    private readonly MemoryStorage _memoryStorage;
    private readonly IConnectionMultiplexer _multiplexerRedis;
    private readonly ILogger<RedisStorage> _logger;
    private readonly DPLJsonConverter _jsonConverter;

    public RedisStorage(MemoryStorage memoryStorage, IConnectionMultiplexer multiplexerRedis, ILogger<RedisStorage> logger, DPLJsonConverter jsonConverter)
    {
        _memoryStorage = memoryStorage;
        _multiplexerRedis = multiplexerRedis;
        _logger = logger;
        _jsonConverter = jsonConverter;
    }
    
    public List<DiscordChannelTracked> GetFromRedisDiscordChannelTracked()
    {
        var redisDb = _multiplexerRedis.GetDatabase(REDIS_DB);
        var server = _multiplexerRedis.GetServer(redisDb.IdentifyEndpoint() ?? _multiplexerRedis.GetEndPoints()[0]);
        var keys = server.Keys(REDIS_DB).ToList();
        
        var results = keys
            .Select(key => redisDb.StringGet(key))
            .Select(redisData => (string) redisData)
            .ToList();

        var list = new List<DiscordChannelTracked>();
        foreach (var res in results)
        {
            var obj = _jsonConverter.ToObject<DiscordChannelTracked>(res);
            list.Add(obj);
        }
        
        return list;
    }
    
    public void LoadFromRedisDiscordChannelTrackedIntoMemory()
    {
        var redisDb = _multiplexerRedis.GetDatabase(REDIS_DB);
        var server = _multiplexerRedis.GetServer(redisDb.IdentifyEndpoint() ?? _multiplexerRedis.GetEndPoints()[0]);
        var keys = server.Keys(REDIS_DB).ToList();
        
        var results = keys
            .Select(key => redisDb.StringGet(key))
            .Select(redisData => (string) redisData)
            .ToList();

        foreach (var res in results)
        {
            var obj = _jsonConverter.ToObject<DiscordChannelTracked>(res);
            _memoryStorage.DiscordChannels.Add(obj);
        }
    }
    
    public NotTextChannelIds LoadFromRedisNotTextChannelIds()
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

    public void SaveIntoRedisMissingPerms(ulong id)
    {
        var redisDb = _multiplexerRedis.GetDatabase(NotTextChannelIds.REDIS_DB);
        redisDb.StringSet(id.ToString(), "no perms", TimeSpan.FromMinutes(10));
    }

    public void SaveIntoRedis(DiscordChannelTracked obj)
    {
        var redisDb = _multiplexerRedis.GetDatabase(REDIS_DB);
        var json = _jsonConverter.FromObject(obj);
        redisDb.StringSet(obj.ChannelId.ToString(), json, TimeSpan.FromDays(7));
    }
    
    public void SaveIntoRedis(NotTextChannelIds obj)
    {
        var redisDb = _multiplexerRedis.GetDatabase(NotTextChannelIds.REDIS_DB);
        var json = _jsonConverter.FromObject(obj);
        redisDb.StringSet(NotTextChannelIds.REDIS_KEY, json, TimeSpan.FromDays(7));
    }
}