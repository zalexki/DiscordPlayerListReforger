using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DiscordPlayerList.Services;

public class RabbitConnectionPublisher
{
    public IConnection Connection;
    public IModel Channel;
    private bool _connectionSuccessful = false;
    private readonly ILogger<RabbitConnectionPublisher> _logger;

    public RabbitConnectionPublisher(ILogger<RabbitConnectionPublisher> logger)
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
            AutomaticRecoveryEnabled = true
        };
        
        var i = 0;
        while (_connectionSuccessful == false && i < 20)
        {
            Thread.Sleep(300 * i);
            i++;

            try
            {
                _logger.LogInformation("publisher TryConnectionWithRetries {I}", i);
                Connection = factory.CreateConnection();
                Channel = Connection.CreateModel();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to connect to rabbitmq");
            }

            if (Connection is {IsOpen: true})
            {
                _connectionSuccessful = true;
            }
        }

        if (Connection is {IsOpen: false})
        {
            throw new Exception("publisher failed to co rabbit");
        }
    }
}