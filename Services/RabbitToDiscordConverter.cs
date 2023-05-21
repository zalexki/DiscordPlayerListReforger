using System.Text;
using discordPlayerList.Models.Request;

namespace discordPlayerList.Services;

public static class RabbitToDiscordConverter
{
    public static string GetPlayerList(ServerGameData data)
    {
        var contentStringBuild = new StringBuilder();

        if (data.PlayerList is null)
        {
            contentStringBuild.Append("no players");
        }
        else
        {
            data.PlayerList.ForEach(x =>
            {
                var emojiIconPlatform = x.Platform == "STEAM" ? "<:steam:1107786853874159737>" : "<:xbox:1107786791999787068>";
                var factionEmoji = x.Faction switch
                {
                    "US" => ":flag_us:",
                    "USSR" => ":flag_ru:", 
                    _ => x.Faction
                };
                contentStringBuild.Append($"{emojiIconPlatform} | {factionEmoji} | {x.Name}");
                contentStringBuild.AppendLine();
            });
        }

        return contentStringBuild.ToString();
    }

    public static string GetWindData(ServerInfo data)
    {
        var contentStringBuild = new StringBuilder();
        contentStringBuild.Append($"Speed: {data.WindSpeed}m/s");
        contentStringBuild.AppendLine();
        contentStringBuild.Append($"Direction: {data.WindDirection}°");
        contentStringBuild.AppendLine();

        return contentStringBuild.ToString();
    }

    public static string GetServerData(ServerInfo data)
    {
        var contentStringBuild = new StringBuilder();
        contentStringBuild.Append("IP: 213.202.254.147");
        contentStringBuild.AppendLine();
        contentStringBuild.Append($"Runtime: {data.UpTime}s");
        contentStringBuild.AppendLine();

        return contentStringBuild.ToString();
    }
}