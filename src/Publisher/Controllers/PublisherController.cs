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
            content = HttpContext.Request.Form.FirstOrDefault().Key;
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
            gameData = JsonConvert.DeserializeObject<ServerGameData>(Utf8ToUtf16(content));
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
    
    public static string Utf8ToUtf16(string utf8String)
    {
        /***************************************************************
         * Every .NET string will store text with the UTF-16 encoding, *
         * known as Encoding.Unicode. Other encodings may exist as     *
         * Byte-Array or incorrectly stored with the UTF-16 encoding.  *
         *                                                             *
         * UTF-8 = 1 bytes per char                                    *
         *    ["100" for the ansi 'd']                                 *
         *    ["206" and "186" for the russian '?']                    *
         *                                                             *
         * UTF-16 = 2 bytes per char                                   *
         *    ["100, 0" for the ansi 'd']                              *
         *    ["186, 3" for the russian '?']                           *
         *                                                             *
         * UTF-8 inside UTF-16                                         *
         *    ["100, 0" for the ansi 'd']                              *
         *    ["206, 0" and "186, 0" for the russian '?']              *
         *                                                             *
         * First we need to get the UTF-8 Byte-Array and remove all    *
         * 0 byte (binary 0) while doing so.                           *
         *                                                             *
         * Binary 0 means end of string on UTF-8 encoding while on     *
         * UTF-16 one binary 0 does not end the string. Only if there  *
         * are 2 binary 0, than the UTF-16 encoding will end the       *
         * string. Because of .NET we don't have to handle this.       *
         *                                                             *
         * After removing binary 0 and receiving the Byte-Array, we    *
         * can use the UTF-8 encoding to string method now to get a    *
         * UTF-16 string.                                              *
         *                                                             *
         ***************************************************************/

        // Get UTF-8 bytes and remove binary 0 bytes (filler)
        List<byte> utf8Bytes = new List<byte>(utf8String.Length);
        foreach (byte utf8Byte in utf8String)
        {
            // Remove binary 0 bytes (filler)
            if (utf8Byte > 0) {
                utf8Bytes.Add(utf8Byte);
            }
        }

        // Convert UTF-8 bytes to UTF-16 string
        return Encoding.UTF8.GetString(utf8Bytes.ToArray());
    }
}
