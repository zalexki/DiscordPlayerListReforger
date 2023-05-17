using System.Runtime.CompilerServices;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace discordPlayerList.Services;

public class RabbitConnection
{
    public readonly IConnection Connection;
    public readonly IModel Channel;

    public RabbitConnection(ILogger<RabbitConnection> logger)
    {
        var host = Environment.GetEnvironmentVariable("RABBIT_HOST");
        var username = Environment.GetEnvironmentVariable("RABBIT_USERNAME");
        var password = Environment.GetEnvironmentVariable("RABBIT_PASSWORD");
        int.TryParse(Environment.GetEnvironmentVariable("RABBIT_PORT"), out var port);

        logger.LogWarning($"host {host}");
        logger.LogWarning($"username {username}");
        logger.LogWarning($"password {password}");
        logger.LogWarning($"port {port}");
        
        var factory = new ConnectionFactory
        {
            HostName = host,
            UserName = username,
            Password = password,
            Port = port,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true
        };
        
        try
        {
            Connection = factory.CreateConnection();
            Channel = Connection.CreateModel();
        }
        catch (BrokerUnreachableException e)
        {
            Console.WriteLine(e);
            Connection = factory.CreateConnection();
            Channel = Connection.CreateModel();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}