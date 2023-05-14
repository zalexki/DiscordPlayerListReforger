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
    public async Task<IActionResult> SendDiscordMsg()
    {
        _logger.LogInformation("healthcheck ok");
        
        return Ok("ok");
    }

    [HttpPost]
    [Consumes("application/x-www-form-urlencoded")]
    public IActionResult PostRabbitMsg()
    {
        var body = HttpContext.Request.Form.FirstOrDefault();
        var gameData = JsonConvert.DeserializeObject<ServerGameData>(body.Key.Trim());
        if (gameData is null)
        {
            return BadRequest();
        }
        
        _logger.LogDebug("received json: {Json}", JsonConvert.SerializeObject(gameData, Formatting.Indented));
        
        _rabbit.Channel.BasicPublish(exchange: string.Empty,
            routingKey: RabbitConsumer.QueueName,
            basicProperties: null,
            body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(gameData)),
            mandatory: true);
        
        _logger.LogInformation("success sent json to rabbit");

        return Ok();
    }
}
