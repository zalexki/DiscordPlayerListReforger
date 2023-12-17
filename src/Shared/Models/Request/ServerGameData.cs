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
    public required List<PlayerInfo> PlayerList { get; set; }
}