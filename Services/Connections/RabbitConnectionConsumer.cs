using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DiscordPlayerList.Services.Connections;

public class RabbitConnectionConsumer
{
    public IConnection Connection;
    private readonly ILogger<RabbitConnectionConsumer> _logger;

    public RabbitConnectionConsumer(ILogger<RabbitConnectionConsumer> logger)
    {
        _logger = logger;

        TryConnectionWithRetries();
    }

    private void TryConnectionWithRetries()
    {
        var host = Environment.GetEnvironmentVariable("RABBIT_HOST");
        var username = Environment.GetEnvironmentVariable("RABBIT_USERNAME");
        var password = Environment.GetEnvironmentVariable("RABBIT_PASSWORD");
        int.TryParse(Environment.GetEnvironmentVariable("RABBIT_PORT"), out var port);

        var factory = new ConnectionFactory
        {
            HostName = host,
            UserName = username,
            Password = password,
            Port = port,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true
        };
        
        var i = 0;
        var mustRetry = true;
        while (mustRetry && i > 20)
        {
            Thread.Sleep(300 * i);
            i++;

            try
            {
                _logger.LogInformation("consumer TryConnectionWithRetries {I}", i);
                Connection = factory.CreateConnection();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to connect to rabbitmq");
            }

            mustRetry = false;
        }
    }
}
