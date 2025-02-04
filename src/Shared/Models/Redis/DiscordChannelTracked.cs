using System;
using Newtonsoft.Json;

namespace DiscordPlayerListShared.Models.Redis;

public class DiscordChannelTracked
{
    public const int REDIS_DB = 1;

    [JsonProperty("isUp")]
    public required bool IsUp { get; set; }
    
    [JsonProperty("channelId")]
    public required ulong ChannelId { get; init; }
    
    [JsonProperty("channelName")]
    public required string ChannelName { get; set; }

    [JsonProperty("ComputedChannelName")]
    public required string ComputedChannelName { get; set; }

    [JsonProperty("firstMessageId")]
    public ulong FirstMessageId { get; set; }

    [JsonProperty("lastUpdate")]
    public required DateTime LastUpdate { get; set; }
}