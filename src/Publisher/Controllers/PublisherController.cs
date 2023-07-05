using System;
using System.Linq;
using System.Text;
using DiscordPlayerListShared.Models.Request;
using DiscordPlayerListShared.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DiscordPlayerListPublisher.Controllers;

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
    public IActionResult Healthcheck()
    {
        _logger.LogInformation("healthcheck ok");
        
        return Ok("ok");
    }
    
    [HttpPost]
    public IActionResult PostRabbitMsg()
    {
        var body = HttpContext.Request.Form.FirstOrDefault();
        _logger.LogInformation("received body: {Body}", body.Key);

        ServerGameData gameData;
        try
        {
            gameData = JsonConvert.DeserializeObject<ServerGameData>(body.Key.Trim());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "failed to deserializeObject");
            return BadRequest();

        }
        if (gameData?.DiscordChannelId is null || gameData?.DiscordChannelName is null)
        {
            return BadRequest();
        }
        
        _rabbit.Channel.BasicPublish(exchange: string.Empty,
            routingKey: ServerGameData.QueueName,
            basicProperties: null,
            body: Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(gameData)),
            mandatory: true);
        
        _logger.LogInformation("success sent json to rabbit");
    
        return Ok();
    }
}
