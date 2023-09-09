using System.Collections.Generic;

namespace DiscordPlayerListConsumer.Models.Redis;

public class NotTextChannelIds
{
    public const int REDIS_DB = 0;
    public const string REDIS_KEY = "notTextChannelList";
    public List<ulong> Ids { get; set; }
}