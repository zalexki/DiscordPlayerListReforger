using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace DiscordPlayerListShared.Models.Request;

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
    public required List<PlayerInfo> PlayerList { get; init; }
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
    [JsonProperty("m_upTime")]
    public required float UpTime { get; init; }

    [Required]
    [JsonProperty("m_timeInGame")]
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
    [JsonProperty("m_name")]
    public required string Name { get; init; }
    
    [Required]
    [JsonProperty("m_platform")]
    public required string Platform { get; init; }
    
    [Required]
    [JsonProperty("m_faction")]
    public required string Faction { get; init; }
    
    [Required]
    [JsonProperty("m_walkedDistance")]
    public required string m_walkedDistance { get; init; }
    
    [Required]
    [JsonProperty("m_drivenDistance")]
    public required string m_drivenDistance { get; init; }
    
    [Required]
    [JsonProperty("m_kills")]
    public required string m_kills { get; init; }
    
    [Required]
    [JsonProperty("m_aiKills")]
    public required string m_aiKills { get; init; }
    
    [Required]
    [JsonProperty("m_deaths")]
    public required string m_deaths { get; init; }
    
    [Required]
    [JsonProperty("m_bandageSelf")]
    public required string m_bandageSelf { get; init; }
    
    [Required]
    [JsonProperty("m_bulletShots")]
    public required string m_bulletShots { get; init; }
    
    [Required]
    [JsonProperty("m_grenadesThrown")]
    public required string m_grenadesThrown { get; init; }
    
    [Required]
    [JsonProperty("m_warCrimes")]
    public required string m_warCrimes { get; init; }
    
    [Required]
    [JsonProperty("m_friendlyPlayerKills")]
    public required string m_friendlyPlayerKills { get; init; }
    
    [Required]
    [JsonProperty("m_friendlyAiKills")]
    public required string m_friendlyAiKills { get; init; }
    
    [Required]
    [JsonProperty("m_sessionDuration")]
    public required string m_sessionDuration { get; init; }
}
