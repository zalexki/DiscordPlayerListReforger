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
                var factionEmoji = ResolveFactionKey(x.Faction);
                contentStringBuild.Append($"{emojiIconPlatform} | {factionEmoji} | {x.Name}");
                contentStringBuild.AppendLine();
            });
        }

        return contentStringBuild.ToString();
    }

    public static string GetWindData(ServerInfo data)
    {
        var contentStringBuild = new StringBuilder();
        var speed = (int) data.WindDirection;
        contentStringBuild.Append($"Direction: {speed.ToString("D3")}°");
        contentStringBuild.AppendLine();
        contentStringBuild.Append($"Speed: {(int) data.WindSpeed}m/s");
        contentStringBuild.AppendLine();

        return contentStringBuild.ToString();
    }

    public static string GetServerData(ServerInfo data)
    {
        var contentStringBuild = new StringBuilder();
        var upTime = new TimeSpan(0, 0, 30, (int) data.UpTime);
        contentStringBuild.Append($"IP: {data.ServerIp}");
        contentStringBuild.AppendLine();
        contentStringBuild.Append($"Runtime: {upTime}");
        contentStringBuild.AppendLine();

        return contentStringBuild.ToString();
    }

    private static string ResolveFactionKey(string factionKey = "")
    {
        return factionKey switch
        {
            "US" => ":flag_us:",
            "USSR" => ":flag_ru:",
            "FIA" => "<:FIA:1109836486536347800>",
            _ => factionKey
        };
    }
    
    public static string ResolveShittyBohemiaMissionName(string missionName = "")
    {
        return missionName switch
        {
            "#AR-Campaign_ScenarioName_Everon" => "Conflict_Everon",
            "#AR-MainMenu_ConflictArland_Name" => "Conflict_Arland",
            "#AR-Editor_Mission_GM_Eden_Name" => "GM_Everon",
            "#AR-Editor_Mission_GM_Arland_Name" => "GM_Arland",
            _ => missionName
        };
    }
}