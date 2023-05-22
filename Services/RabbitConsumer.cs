using System.Text;
using DiscordPlayerList.Models;
using discordPlayerList.Models.Request;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace discordPlayerList.Services;

public class RabbitConsumer : BackgroundService
{
    private readonly DiscordChannelList _listOfChannels;
    private readonly DiscordClient _discord;
    private readonly ILogger<RabbitConsumer> _logger;
    private readonly IModel _channel;
    public const string QueueName = "arma_reforger_discord_player_list";

    public RabbitConsumer(ILogger<RabbitConsumer> logger, RabbitConnection rabbitConnection, DiscordClient discord, DiscordChannelList listOfChannels)
    {
        _logger = logger;
        _discord = discord;
        _listOfChannels = listOfChannels;
        _channel = rabbitConnection.Connection.CreateModel();
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

            await _discord.SendMessageFromGameData(data);
        }
        else
        {
            _logger.LogError("failed to deserialize message: {RabbitMessage}", rabbitMessage);
        }
    }
    
}