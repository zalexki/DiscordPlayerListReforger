using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DiscordPlayerList.Models;
using DiscordPlayerList.Models.Request;
using DiscordPlayerList.Services.Connections;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DiscordPlayerList.Services.BackgroundService;

public class RabbitConsumer : Microsoft.Extensions.Hosting.BackgroundService
{
    private readonly MemoryStorage _listOfChannels;
    private readonly DiscordHelper _discord;
    private readonly ILogger<RabbitConsumer> _logger;
    private readonly IModel _channel;
    public const string QueueName = "arma_reforger_discord_player_list";

    public RabbitConsumer(ILogger<RabbitConsumer> logger, RabbitConnectionConsumer rabbitConnectionConsumer, DiscordHelper discord, MemoryStorage listOfChannels)
    {
        _logger = logger;
        _discord = discord;
        _listOfChannels = listOfChannels;
        _channel = rabbitConnectionConsumer.Connection.CreateModel();
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _channel.QueueDeclare(queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            _channel.BasicQos(0, 3, false); //need to learn more about this
        
            var c = new AsyncEventingBasicConsumer(_channel);
            c.Received += OnReceived;
            _channel.BasicConsume(queue: QueueName, autoAck: true, consumer: c);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "failed to StartConsumeRabbit");
        }
        
        _logger.LogInformation($"RabbitConsumer started, Queue [{QueueName}] is waiting for messages.");
        return Task.CompletedTask;
    }

    private async Task OnReceived(object model, BasicDeliverEventArgs eventArgs)
    {
        try
        {
            var rabbitMessage = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            _logger.LogInformation("RabbitConsumer received: {RabbitMessage}", rabbitMessage);
        

            var data = JsonConvert.DeserializeObject<ServerGameData>(rabbitMessage);
            if (data is not null)
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
                    _listOfChannels.DiscordChannels.Add(new DiscordChannelTracked()
                    {
                        IsUp = true,
                        ChannelId = data.DiscordChannelId,
                        ChannelName = data.DiscordChannelName,
                        LastUpdate = DateTime.UtcNow
                    });
                }

                var success = await _discord.SendMessageFromGameData(data);
                if (success)
                {
                    _logger.LogInformation("RabbitConsumer finished successfully to consume: {RabbitMessage}", rabbitMessage);
                } else {
                    _logger.LogInformation("RabbitConsumer finished failed to consume: {RabbitMessage}", rabbitMessage);
                }
            }
            else
            {
                _logger.LogError("failed to deserialize message: {RabbitMessage}", rabbitMessage);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "OnReceived failed");
        }
    }
}