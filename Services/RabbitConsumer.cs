using System.Text;
using discordPlayerList.Models.Request;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace discordPlayerList.Services;

public class RabbitConsumer : BackgroundService
{
    private readonly DiscordClient _discord;
    private readonly ILogger<RabbitConsumer> _logger;
    private readonly IModel _channel;
    private const string QueueName = "arma_reforger_discord_player_list";

    public RabbitConsumer(ILogger<RabbitConsumer> logger, RabbitConnection rabbitConnection, DiscordClient discord)
    {
        _logger = logger;
        _discord = discord;
        _channel = rabbitConnection.Connection.CreateModel();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await StartConsumeRabbit();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "failed to StartConsumeRabbit");
        }
        
        _logger.LogDebug($"RabbitConsumer started, Queue [{QueueName}] is waiting for messages.");
    }

    private async Task OnReceived(object model, BasicDeliverEventArgs eventArgs)
    {
        var rabbitMessage = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
        _logger.LogDebug($"RabbitConsumer received: {rabbitMessage}");

        var data = JsonConvert.DeserializeObject<ServerGameData>(rabbitMessage);
        if (data is not null)
        {
            await _discord.SendMessageFromGameData(data);
        }
        else
        {
            _logger.LogError($"failed to deserialize message: {rabbitMessage}");
            _channel.BasicNack(deliveryTag: eventArgs.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        _channel.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);
    }
    private Task StartConsumeRabbit()
    {
        _channel.QueueDeclare(queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        // _channel.BasicQos(0, 1, false); need to learn more about this
        
        try
        {
            var c = new AsyncEventingBasicConsumer(_channel);
            c.Received += OnReceived;
            _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: c);
        }
        catch (Exception e)
        {
            _logger.LogError(e,"fucked up");
        }

        return Task.CompletedTask;
    }
}