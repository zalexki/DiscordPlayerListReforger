using System;
using System.IO;
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
        string content;
        try
        {
            content = HttpContext.Request.Form.FirstOrDefault().Key;
            _logger.LogInformation("received body: {Body}", content);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "content type shit ?");
            var request = HttpContext.Request;
            var query = HttpContext.Request.Query;

            using var stream = new StreamReader(HttpContext.Request.Body);
            var bodyStream = stream.ReadToEnd();

            _logger.LogError("request data: {Data}",JsonConvert.SerializeObject(request, Formatting.Indented));
            _logger.LogError("query data: {Data}",JsonConvert.SerializeObject(query, Formatting.Indented));
            _logger.LogError("body stream data: {Data}",JsonConvert.SerializeObject(bodyStream, Formatting.Indented));

            return BadRequest();
        }

        ServerGameData gameData;
        try
        {
            gameData = JsonConvert.DeserializeObject<ServerGameData>(content);
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
