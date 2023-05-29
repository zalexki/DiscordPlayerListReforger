using System.Collections.Generic;
using DiscordPlayerListConsumer.Models;

namespace DiscordPlayerListConsumer.Services;

public class MemoryStorage
{
    public readonly List<DiscordChannelTracked> DiscordChannels = new();
}