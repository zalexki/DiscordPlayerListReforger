using RabbitMQ.Client;

namespace discordPlayerList.Services;

public class RabbitConnection
{
    public readonly IConnection Connection;
    public readonly IModel Channel;

    public RabbitConnection(ILogger<RabbitConnection> logger)
    {
        int.TryParse(Environment.GetEnvironmentVariable("RABBIT_PORT"), out var port);

        var factory = new ConnectionFactory
        {
            HostName = Environment.GetEnvironmentVariable("RABBIT_HOST"),
            UserName = Environment.GetEnvironmentVariable("RABBIT_USERNAME"),
            Password = Environment.GetEnvironmentVariable("RABBIT_PASSWORD"),
            Port = port,
            DispatchConsumersAsync = true,
        };
        Connection = factory.CreateConnection();
        Channel = Connection.CreateModel();

        logger.LogDebug("RabbitConnection done");
    }
}