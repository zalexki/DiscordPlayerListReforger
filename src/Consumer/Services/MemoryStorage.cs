using System;
using System.Collections.Generic;
using Discord;
using DiscordPlayerListShared.Models.Redis;

namespace DiscordPlayerListConsumer.Services;

public class MemoryStorage
{
    public readonly Dictionary<ulong, int> DiscordRetryChannels = new();
    public readonly Dictionary<ulong, ITextChannel> DiscordTextChannels = new();
    public readonly List<DiscordChannelTracked> DiscordChannels = new();
    
    public ulong BotUserId { get; set; }
    
    public TimeSpan waitBeforeSendChannelName { get; set; }
    public TimeSpan waitBeforeSendChannelMessage { get; set; }
    public TimeSpan waitBeforeGetChannel { get; set; }
}