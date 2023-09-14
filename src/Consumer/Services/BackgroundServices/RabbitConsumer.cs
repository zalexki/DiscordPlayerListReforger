using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DiscordPlayerListShared.Models.Request;
using DiscordPlayerListConsumer.Models;
using DiscordPlayerListConsumer.Services.Helpers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DiscordPlayerListShared.Services;
using StackExchange.Redis;
using DiscordPlayerListShared.Converter;
using DiscordPlayerListConsumer.Models.Redis;

namespace DiscordPlayerListConsumer.Services.BackgroundServices;

public class RabbitConsumer : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly RabbitConnection _rabbitConnectionConsumer;
    private readonly MemoryStorage _listOfChannels;
    private readonly DiscordHelper _discord;
    private readonly IConnectionMultiplexer _multiplexerRedis;
    private readonly ILogger<RabbitConsumer> _logger;
    private readonly DPLJsonConverter _jsonConverter;

    private const int REDIS_DB = 1;

    public RabbitConsumer(ILogger<RabbitConsumer> logger, DiscordHelper discord, MemoryStorage listOfChannels, 
        RabbitConnection rabbitConnectionConsumer, IConnectionMultiplexer multiplexerRedis, DPLJsonConverter jsonConverter)
    {
        _logger = logger;
        _discord = discord;
        _listOfChannels = listOfChannels;
        _rabbitConnectionConsumer = rabbitConnectionConsumer;
        _multiplexerRedis = multiplexerRedis;
        _jsonConverter = jsonConverter;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LoadRedisIntoMemory();

        try
        {
            for (var i = 0; i < 1; i++)
            {
                var channel = _rabbitConnectionConsumer.Connection.CreateModel();
                channel.QueueDeclare(queue: ServerGameData.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
                channel.BasicQos(0, 1, false);
            
                var c = new AsyncEventingBasicConsumer(channel);
                c.Received += OnReceived;

                channel.BasicConsume(queue: ServerGameData.QueueName, autoAck: true, consumer: c);
                _logger.LogInformation("RabbitConsumer {I} started, Queue [{ArmaReforgerDiscordPlayerList}] is waiting for messages", i, ServerGameData.QueueName);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "failed to StartConsumeRabbit");
        }
        
        return Task.CompletedTask;
    }

    private async Task OnReceived(object model, BasicDeliverEventArgs eventArgs)
    {
        try
        {
            var rabbitMessage = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            _logger.LogInformation("RabbitConsumer received: {RabbitMessage}", rabbitMessage);

            var data = JsonConvert.DeserializeObject<ServerGameData>(rabbitMessage);
            if (data is null)
            {
                _logger.LogError("failed to deserialize message: {RabbitMessage}", rabbitMessage);

                return;
            }

            if (IsInNotATextChannelList(data.DiscordChannelId))
            {
                _logger.LogWarning("skipped not a text channel id : {DiscordChannelId}", data.DiscordChannelId);

                return;
            }

            addOrUpdateChannelInRedis(data);

            if (_listOfChannels.waitBeforeSendChannelMessage.TotalMilliseconds > 0) await Task.Delay(_listOfChannels.waitBeforeSendChannelMessage);
            if (_listOfChannels.waitBeforeSendChannelName.TotalMilliseconds > 0) await Task.Delay(_listOfChannels.waitBeforeSendChannelName);
            
            if (await _discord.SendMessageFromGameData(data))
            {
                _logger.LogInformation("RabbitConsumer finished successfully to consume: {Id}", data.DiscordChannelId);
            } else {
                _logger.LogInformation("RabbitConsumer finished failed to consume: {Id}", data.DiscordChannelId);
            }
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "OnReceived failed");
        }
    }

    private void addOrUpdateChannelInRedis(ServerGameData data)
    {
        var existingChannel = _listOfChannels.DiscordChannels.SingleOrDefault(x => x.ChannelId == data.DiscordChannelId);
        if (existingChannel is not null)
        {
            existingChannel.ChannelName = data.DiscordChannelName;
            existingChannel.IsUp = true;
            existingChannel.LastUpdate = DateTime.UtcNow;
        }
        else
        {
            existingChannel = new DiscordChannelTracked()
            {
                IsUp = true,
                ChannelId = data.DiscordChannelId,
                ChannelName = data.DiscordChannelName,
                ComputedChannelName = data.DiscordChannelName,
                LastUpdate = DateTime.UtcNow
            };
            _listOfChannels.DiscordChannels.Add(existingChannel);
        }
        SaveIntoRedis(existingChannel);
    }
    
    private void LoadRedisIntoMemory()
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
            _listOfChannels.DiscordChannels.Add(obj);
        }
    }

    private void SaveIntoRedis(DiscordChannelTracked obj)
    {
        var redisDb = _multiplexerRedis.GetDatabase(REDIS_DB);
        var json = _jsonConverter.FromObject(obj);
        redisDb.StringSet(obj.ChannelId.ToString(), json, TimeSpan.FromDays(7));
    }

    private bool IsInNotATextChannelList(ulong id)
    {
        var redisDb = _multiplexerRedis.GetDatabase(NotTextChannelIds.REDIS_DB);
        var data = redisDb.StringGet(NotTextChannelIds.REDIS_KEY);
        if (data.IsNull)
        {
            return false;
        }

        var obj = _jsonConverter.ToObject<NotTextChannelIds>(data.ToString());
        
        return obj.Ids is not null && obj.Ids.Contains(id);
    }
}