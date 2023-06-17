using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace DiscordPlayerList.Models.Request;

public class ServerGameData
{
    [Required]
    [JsonProperty("m_discordChannelId")]
    public required ulong DiscordChannelId { get; init; }
    
    [Required]
    [JsonProperty("m_discordChannelName")]
    public required string DiscordChannelName { get; init; }
    
    [Required]
    [JsonProperty("m_discordMessageTitle")]
    public required string DiscordMessageTitle { get; init; }
    
    [Required]
    [JsonProperty("m_serverInfos")]
    public required ServerInfo ServerInfo { get; init; }
    
    [Required]
    [JsonProperty("m_players")]
    public required List<PlayerInfo>? PlayerList { get; init; }
}

public class ServerInfo
{
    [Required]
    [JsonProperty("m_serverIP")]
    public required string ServerIp { get; init; }
    
    [Required]
    [JsonProperty("m_missionName")]
    public required string MissionName { get; init; }
    
    [Required]
    [JsonProperty("m_timeInGame")]
    public required float UpTime { get; init; }

    [Required]
    [JsonProperty("timeInGame")]
    public required string TimeInGame { get; init; }
    
    [Required]
    [JsonProperty("m_playerCount")]
    public required int PlayerCount { get; init; }
    
    [Required]
    [JsonProperty("m_maxPlayerCount")]
    public required int MaxPlayerCount { get; init; }
    
    [Required]
    [JsonProperty("m_windSpeed")]
    public required float WindSpeed { get; init; }
    
    [Required]
    [JsonProperty("m_windDirection")]
    public required float WindDirection { get; init; }
    
    [Required]
    [JsonProperty("m_rainIntensity")]
    public required float RainIntensity { get; init; }
}

public class PlayerInfo
{
    [Required]
    [JsonProperty("name")]
    public required string Name { get; init; }
    
    [Required]
    [JsonProperty("platform")]
    public required string Platform { get; init; }
    
    [Required]
    [JsonProperty("faction")]
    public required string Faction { get; init; }
}
