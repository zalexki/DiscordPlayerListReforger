using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace DiscordPlayerListShared.Models.Request;

public class ServerGameData
{
    public const string QueueName = "arma_reforger_discord_player_list";
    
    [Required]
    [JsonProperty("discordChannelId")]
    public required ulong DiscordChannelId { get; init; }
    
    [Required]
    [JsonProperty("discordChannelName")]
    public required string DiscordChannelName { get; init; }
    
    [Required]
    [JsonProperty("discordMessageTitle")]
    public required string DiscordMessageTitle { get; init; }
    
    [Required]
    [JsonProperty("serverInfos")]
    public required ServerInfo ServerInfo { get; init; }
    
    [Required]
    [JsonProperty("players")]
    public required List<PlayerInfo> PlayerList { get; init; }
}

public class ServerInfo
{
    [Required]
    [JsonProperty("serverIP")]
    public required string ServerIp { get; init; }
    
    [Required]
    [JsonProperty("missionName")]
    public required string MissionName { get; init; }
    
    [Required]
    [JsonProperty("upTime")]
    public required float UpTime { get; init; }

    [Required]
    [JsonProperty("timeInGame")]
    public required string TimeInGame { get; init; }
    
    [Required]
    [JsonProperty("playerCount")]
    public required int PlayerCount { get; init; }
    
    [Required]
    [JsonProperty("maxPlayerCount")]
    public required int MaxPlayerCount { get; init; }
    
    [Required]
    [JsonProperty("windSpeed")]
    public required float WindSpeed { get; init; }
    
    [Required]
    [JsonProperty("windDirection")]
    public required float WindDirection { get; init; }
    
    [Required]
    [JsonProperty("rainIntensity")]
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

    [Required]
    [JsonProperty("kills")]
    public required string Kills { get; init; }
    
    [Required]
    [JsonProperty("aiKills")]
    public required string KillsAi { get; init; }
    
    [Required]
    [JsonProperty("deaths")]
    public required string Deaths { get; init; }
    
    [Required]
    [JsonProperty("friendlyPlayerKills")]
    public required string FriendlyPlayerKills { get; init; }
    
    [Required]
    [JsonProperty("friendlyAiKills")]
    public required string FriendlyAiKills { get; init; }
}
