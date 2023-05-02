using Newtonsoft.Json;

namespace discordPlayerList.Models;

public class RequestJsonFromGame
{
    [JsonProperty(nameof(serverId))]
    public string serverId { get; init; }
    
    [JsonProperty(nameof(playerCount))]
    public int playerCount { get; init; }
    
    [JsonProperty(nameof(maxPlayerCount))]
    public int maxPlayerCount { get; init; }
    
    [JsonProperty(nameof(playerList))]
    public List<string> playerList { get; init; }
}