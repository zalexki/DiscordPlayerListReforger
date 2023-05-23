using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DiscordPlayerList.Services;

public class RabbitConnectionConsumer
{
    public IConnection Connection;
    private bool _connectionSuccessful = false;
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
        _logger.LogInformation($"host {host}");
        _logger.LogInformation($"username {username}");
        _logger.LogInformation($"password {password}");
        _logger.LogInformation($"port {port}");
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
        while (_connectionSuccessful == false || i < 20)
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

            if (Connection is {IsOpen: true})
            {
                _connectionSuccessful = true;
            }
            
        }

        if (Connection is {IsOpen: false})
        {
            throw new Exception("consumer failed to co rabbit");
        }
    }
}