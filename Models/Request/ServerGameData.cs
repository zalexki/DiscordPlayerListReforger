using Newtonsoft.Json;

namespace discordPlayerList.Models.Request;

public class ServerGameData
{
    [JsonProperty("discordChannelId")]
    public ulong? DiscordChannelId { get; init; }
    
    [JsonProperty("discordChannelName")]
    public string? DiscordChannelName { get; init; }
    
    [JsonProperty("discordMessageTitle")]
    public string? DiscordMessageTitle { get; init; }
    
    [JsonProperty("serverInfos")]
    public ServerInfo ServerInfo { get; init; }
    
    [JsonProperty("playerList")]
    public List<PlayerInfo>? PlayerList { get; init; }
}

public class ServerInfo
{
    [JsonProperty("missionName")]
    public string? MissionName { get; init; }
    
    [JsonProperty("upTime")]
    public string? UpTime { get; init; }

    [JsonProperty("timeInGame")]
    public float TimeInGame { get; init; }
    
    [JsonProperty("playerCount")]
    public int PlayerCount { get; init; }
    
    [JsonProperty("maxPlayerCount")]
    public int MaxPlayerCount { get; init; }
    
    [JsonProperty("windSpeed")]
    public float WindSpeed { get; init; }
    
    [JsonProperty("windDirection")]
    public float WindDirection { get; init; }
    
    [JsonProperty("rainIntensity")]
    public float RainIntensity { get; init; }
    

}

public class PlayerInfo
{
    [JsonProperty("name")]
    public string? Name { get; init; }
    
    [JsonProperty("platform")]
    public string? Platform { get; init; }
    
    [JsonProperty("faction")]
    public string? Faction { get; init; }
}