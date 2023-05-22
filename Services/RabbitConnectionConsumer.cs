using RabbitMQ.Client;

namespace DiscordPlayerList.Services;

public class RabbitConnectionConsumer
{
    public IConnection Connection;
    public IModel Channel;
    private bool _connectionSuccessful = false;
    private readonly ILogger<RabbitConnectionConsumer> _logger;

    public RabbitConnectionConsumer(ILogger<RabbitConnectionConsumer> logger)
    {
        _logger = logger;

        TryConnectionWithRetries();
    }

    protected void TryConnectionWithRetries()
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
        while (_connectionSuccessful == false || i >= 10)
        {
            Thread.Sleep(1000 * i);
            try
            {
                Connection = factory.CreateConnection();
                Channel = Connection.CreateModel();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failed to connect to rabbitmq");
                i++;
            }

            if (Connection is {IsOpen: true})
            {
                _connectionSuccessful = true;
            }
        }

        if (Connection is {IsOpen: false})
        {
            throw new Exception("failed to co rabbit");
        }
    }
}