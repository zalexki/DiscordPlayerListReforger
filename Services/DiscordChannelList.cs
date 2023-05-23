using System.Collections.Generic;
using DiscordPlayerList.Models;

namespace DiscordPlayerList.Services;

public class DiscordChannelList
{
    public readonly List<DiscordChannelTracked> DiscordChannels = new();
}