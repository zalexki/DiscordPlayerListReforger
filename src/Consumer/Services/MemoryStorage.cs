using System.Collections.Generic;
using DiscordPlayerList.Models;

namespace DiscordPlayerList.Services;

public class MemoryStorage
{
    public readonly List<DiscordChannelTracked> DiscordChannels = new();
}