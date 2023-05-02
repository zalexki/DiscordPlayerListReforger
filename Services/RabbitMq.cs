using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace discordPlayerList.Services;

public class RabbitMq
{
    public IModel channel { get; set; }
    
    public RabbitMq()
    {
        // var factory = new ConnectionFactory
        // {
        //     HostName = Environment.GetEnvironmentVariable("RABBIT_HOST"),
        //     UserName = Environment.GetEnvironmentVariable("RABBIT_USERNAME"),
        //     Password = Environment.GetEnvironmentVariable("RABBIT_PASSWORD")
        // };
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "guest",
            Password = "guest"
        };
        var te = Environment.GetEnvironmentVariable("RABBIT_PASSWORD");
        channel = factory.CreateConnection().CreateModel();
        channel.QueueDeclare(queue: "player_list",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        
        var consumer = new EventingBasicConsumer(channel);
        Console.WriteLine("connected rabbitMq");

        consumer.Received += (model, ea) =>
        {
            byte[] body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine($" [x] Received {message}");

            // here channel could also be accessed as ((EventingBasicConsumer)sender).Model
            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        };
        channel.BasicConsume(queue: "player_list",
            autoAck: false,
            consumer: consumer);
    }
}