using System;

namespace DiscordPlayerListConsumer.Models;

public class DiscordChannelTracked
{
    public required bool IsUp { get; set; }
    public required ulong ChannelId { get; init; }
    public required string ChannelName { get; set; }
    public required DateTime LastUpdate { get; set; }
}