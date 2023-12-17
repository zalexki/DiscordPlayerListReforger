using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace DiscordPlayerListShared.Models.Request;

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