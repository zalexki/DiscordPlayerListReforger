using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DiscordPlayerListConsumer.Services;

public class JsonConverter
{
    public Encoding Encoding { get; init; } = Encoding.UTF8;

    private JsonSerializer Serializer { get; init; } = new ()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        },
        ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
        NullValueHandling = NullValueHandling.Ignore
    };

    public string FromObject<T>(T value)
    {
        var sb = new StringBuilder();
        using (var writer = new JsonTextWriter(new StringWriter(sb)))
        { 
            Serializer.Serialize(writer, value);
        }

        return sb.ToString();
    }


    public T ToObject<T>(string value)
    {
        using (var reader = new JsonTextReader(new StringReader(value)))
        {
            return Serializer.Deserialize<T>(reader);
        }
    }
}