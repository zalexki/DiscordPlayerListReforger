using Newtonsoft.Json;

namespace discordPlayerList.Models.Request;

public class ServerGameData
{
    [JsonProperty("missionName")]
    public string MissionName { get; init; }

    [JsonProperty("discordChannelId")]
    public ulong DiscordChannelId { get; init; }
    
    [JsonProperty("discordChannelName")]
    public string DiscordChannelName { get; init; }
    
    [JsonProperty("playerCount")]
    public int PlayerCount { get; init; }
    
    [JsonProperty("maxPlayerCount")]
    public int MaxPlayerCount { get; init; }
    
    [JsonProperty("playerList")]
    public List<PlayerInfo> PlayerList { get; init; }
}

public class PlayerInfo
{
    [JsonProperty("name")]
    public string Name { get; init; }
    
    [JsonProperty("platform")]
    public string Platform { get; init; }
}