using System.Text;
using discordPlayerList.Models;
using discordPlayerList.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace discordPlayerList.Controllers;

[ApiController]
[Route("/publish")]
public class PublisherController : ControllerBase
{
    private readonly ILogger<PublisherController> _logger;
    private readonly RabbitMq _rabbit;

    public PublisherController(ILogger<PublisherController> logger, RabbitMq rabbit)
    {
        _logger = logger;
        _rabbit = rabbit;
    }

    [HttpGet]
    public async Task<IActionResult> SendDiscordMsg()
    {
        _logger.LogInformation("healthcheck");

        return Ok("ok");
    }

    [HttpPost]
    public async Task<IActionResult> PostRabbitMsg([FromBody] RequestJsonFromGame gameData)
    {
        _logger.LogInformation("log received json: {Json}", JsonConvert.SerializeObject(gameData, Formatting.Indented));
        var eev = Environment.GetEnvironmentVariable("RABBIT_HOST");

        const string message = "Hello World!";
        var body = Encoding.UTF8.GetBytes(message);

        _rabbit.channel.BasicPublish(exchange: string.Empty,
            routingKey: "player_list",
            basicProperties: null,
            body: body,
            mandatory: true);
        
        Console.WriteLine($"Sent {message}");

        return Ok();
    }
}
