using System;
using Discord;
using Newtonsoft.Json;

namespace DiscordPlayerListConsumer.Models;

public class DiscordChannelTracked
{
    [JsonProperty("isUp")]
    public required bool IsUp { get; set; }
    
    [JsonProperty("channelId")]
    public required ulong ChannelId { get; init; }
    
    [JsonProperty("channelName")]
    public required string ChannelName { get; set; }

    [JsonProperty("ComputedchannelName")]
    public required string ComputedChannelName { get; set; }

    [JsonProperty("firstMessageId")]
    public ulong FirstMessageId { get; set; }

    [JsonProperty("lastUpdate")]
    public required DateTime LastUpdate { get; set; }
    
    public ITextChannel? chanText { get; set; }
}