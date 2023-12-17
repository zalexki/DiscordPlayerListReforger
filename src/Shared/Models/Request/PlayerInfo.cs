using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace DiscordPlayerListShared.Models.Request;


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
    public required float Kills { get; init; }
    
    [Required]
    [JsonProperty("aiKills")]
    public required float KillsAi { get; init; }
    
    [Required]
    [JsonProperty("deaths")]
    public required float Deaths { get; init; }
    
    [Required]
    [JsonProperty("friendlyPlayerKills")]
    public required float FriendlyPlayerKills { get; init; }
    
    [Required]
    [JsonProperty("friendlyAiKills")]
    public required float FriendlyAiKills { get; init; }
}
