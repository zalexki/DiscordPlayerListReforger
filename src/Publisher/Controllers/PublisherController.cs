using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public async Task<IActionResult> PostRabbitMsg()
    {
        string content;
        try
        {
            var firstParam = HttpContext.Request.Form.FirstOrDefault();
            content = firstParam.Key;
            if (firstParam.Value != string.Empty)
            {
                content = content + firstParam.Value;
            }
            
            _logger.LogInformation("received body: {Body}", content);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "content type shit ?");
            var request = HttpContext.Request;

            using var stream = new StreamReader(HttpContext.Request.Body);
            var bodyStream = await stream.ReadToEndAsync();
            var jsonConfig = new JsonSerializerSettings{ ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
            
            //_logger.LogError("request data: {Data}", JsonConvert.SerializeObject(request, Formatting.Indented, jsonConfig));
            _logger.LogError("query data: {Data}", JsonConvert.SerializeObject(request.Query, Formatting.Indented, jsonConfig));
            _logger.LogError("headers data: {Data}", JsonConvert.SerializeObject(request.Headers, Formatting.Indented, jsonConfig));
            _logger.LogError("scheme data: {Data}", JsonConvert.SerializeObject(request.Scheme, Formatting.Indented, jsonConfig));
            _logger.LogError("contentType data: {Data}", JsonConvert.SerializeObject(request.ContentType, Formatting.Indented, jsonConfig));
            _logger.LogError("protocol data: {Data}", JsonConvert.SerializeObject(request.Protocol, Formatting.Indented, jsonConfig));
            _logger.LogError("body stream data: {Data}", JsonConvert.SerializeObject(bodyStream, Formatting.Indented, jsonConfig));

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
