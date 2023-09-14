using System;
using System.Collections.Generic;
using DiscordPlayerListConsumer.Models;

namespace DiscordPlayerListConsumer.Services;

public class MemoryStorage
{
    public readonly List<DiscordChannelTracked> DiscordChannels = new();
    public ulong BotUserId { get; set; }
    public TimeSpan waitBeforeSendChannelName { get; set; }
    public TimeSpan waitBeforeSendChannelMessage { get; set; }
}