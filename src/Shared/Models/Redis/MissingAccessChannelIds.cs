using System.Collections.Generic;

namespace DiscordPlayerListConsumer.Models.Redis;

public class MissingAccessChannelIds
{
    public const int REDIS_DB = 2;
    
    public ulong Id { get; set; }
}