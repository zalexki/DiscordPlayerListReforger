using System.Text;
using discordPlayerList.Models.Request;
using discordPlayerList.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace discordPlayerList.Controllers;

[ApiController]
[Route("/publish")]
public class PublisherController : ControllerBase
{
    private readonly ILogger<PublisherController> _logger;
    private readonly RabbitConnection _rabbit;

    public PublisherController(ILogger<PublisherController> logger, RabbitConnection rabbit)
    {
        _logger = logger;
        _rabbit = rabbit;
    }

    [HttpGet]
    public IActionResult SendDiscordMsg()
    {
        _logger.LogInformation("healthcheck ok");
        
        return Ok("ok");
    }

    [HttpPost]
    [Consumes("application/x-www-form-urlencoded")]
    public IActionResult PostRabbitMsg()
    {
        var body = HttpContext.Request.Form.FirstOrDefault();
        _logger.LogInformation("received body: {Body}", body.Key);
    
        var gameData = JsonConvert.DeserializeObject<ServerGameData>(body.Key.Trim());
        if (gameData?.DiscordChannelId is null || gameData?.DiscordChannelName is null)
        {
            return BadRequest();
        }
        
        _rabbit.Channel.BasicPublish(exchange: string.Empty,
            routingKey: RabbitConsumer.QueueName,
            basicProperties: null,
            body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(gameData)),
            mandatory: true);
        
        _logger.LogInformation("success sent json to rabbit");
    
        return Ok();
    }
}
