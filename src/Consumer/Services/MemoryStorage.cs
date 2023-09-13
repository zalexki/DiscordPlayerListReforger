using System.Collections.Generic;
using DiscordPlayerListConsumer.Models;

namespace DiscordPlayerListConsumer.Services;

public class MemoryStorage
{
    public readonly List<DiscordChannelTracked> DiscordChannels = new();
    public ulong BotUserId { get; set; }
    public int waitBeforeSendChannelName { get; set; }
    public int waitBeforeSendChannelMessage { get; set; }
}