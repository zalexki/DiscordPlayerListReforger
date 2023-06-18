using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace DiscordPlayerListShared.Models.Request;

public class ServerGameData
{
    public const string QueueName = "arma_reforger_discord_player_list";
    
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
    public required string WalkedDistance { get; init; }
    
    [Required]
    [JsonProperty("m_drivenDistance")]
    public required string DrivenDistance { get; init; }
    
    [Required]
    [JsonProperty("m_kills")]
    public required string Kills { get; init; }
    
    [Required]
    [JsonProperty("m_aiKills")]
    public required string KillsAi { get; init; }
    
    [Required]
    [JsonProperty("m_deaths")]
    public required string Deaths { get; init; }
    
    [Required]
    [JsonProperty("m_bandageSelf")]
    public required string BandageSelf { get; init; }
    
    [Required]
    [JsonProperty("m_bulletShots")]
    public required string BulletShots { get; init; }
    
    [Required]
    [JsonProperty("m_grenadesThrown")]
    public required string GrenadesThrown { get; init; }
    
    [Required]
    [JsonProperty("m_warCrimes")]
    public required string WarCrimes { get; init; }
    
    [Required]
    [JsonProperty("m_friendlyPlayerKills")]
    public required string FriendlyPlayerKills { get; init; }
    
    [Required]
    [JsonProperty("m_friendlyAiKills")]
    public required string FriendlyAiKills { get; init; }
    
    [Required]
    [JsonProperty("m_sessionDuration")]
    public required string SessionDuration { get; init; }
}
