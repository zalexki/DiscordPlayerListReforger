using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using DiscordPlayerListShared.Models.Request;
using Microsoft.Extensions.Logging;

namespace DiscordPlayerListConsumer.Services.Helpers;

public class StringConverted
{
    public string data { get; set; }
    public int count { get; set; }
}

public static class RabbitToDiscordConverter
{
    public static string GetPlayerFriendlyKills(ServerGameData data)
    {
        var contentStringBuild = new StringBuilder();
        var i = 0;
        foreach (var player in data.PlayerList)
        {
            i++;
            if (i > 45)
            {
                break;
            }


            var ratio = Math.Round(player.KillsDeathRatio, 2, MidpointRounding.AwayFromZero);
            contentStringBuild.Append($"{player.TeamKills} | {ratio}");
            contentStringBuild.AppendLine();
        }

        if (contentStringBuild.Length == 0)
        {
            contentStringBuild.Append("empty");
        }
        
        return contentStringBuild.ToString();
    }
    
    public static string GetPlayerExtrasPlatformFaction(ServerGameData data)
    {
        var contentStringBuild = new StringBuilder();
        var i = 0;
        foreach (var player in data.PlayerList)
        {
            i++;
            if (i > 45)
            {
                break;
            }
            var emojiIconPlatform = player.Platform == "STEAM" ? "<:steam:1107786853874159737>" : "<:xbox:1107786791999787068>";
            var factionEmoji = ResolveFactionKey(player.Faction);
            
            contentStringBuild.Append($"{factionEmoji}");
            contentStringBuild.AppendLine();
        }
        
        if (contentStringBuild.Length == 0)
        {
            contentStringBuild.Append("empty");
        }
        
        return contentStringBuild.ToString();
    }
    
    public static string GetPlayerExtrasKillDeath(ServerGameData data)
    {
        var contentStringBuild = new StringBuilder();
        var i = 0;
        foreach (var player in data.PlayerList)
        {
            i++;
            if (i > 45)
            {
                break;
            }

            contentStringBuild.Append($" {player.Kills} | {player.Deaths}");
            contentStringBuild.AppendLine();
        }
        
        if (contentStringBuild.Length == 0)
        {
            contentStringBuild.Append("empty");
        }
        
        return contentStringBuild.ToString();
    }
    
    public static StringConverted GetPlayerList(ServerGameData data)
    {
        var contentStringBuild = new StringBuilder();
        var andMoreText = "and more ...";
        var i = 0;
        foreach (var player in data.PlayerList)
        {
            i++;
            if (i > 45)
            {
                break;
            }
            
            if (contentStringBuild.Length + player.Name.Length >= DiscordHelper.DISCORD_FIELD_MAX_LENGTH)
            {
                if (contentStringBuild.Length + andMoreText.Length >= DiscordHelper.DISCORD_FIELD_MAX_LENGTH)
                {
                    if (contentStringBuild.Length <= DiscordHelper.DISCORD_FIELD_MAX_LENGTH - 3)
                    {
                        contentStringBuild.Append("...");
                    }
                }
                else
                {
                    contentStringBuild.Append("and more ...");
                }
                break;
            }
            else
            {
                contentStringBuild.Append(player.Name);
                contentStringBuild.AppendLine();
            }
        }

        if (false == data.PlayerList?.Any())
        {
            contentStringBuild.Append("no players");
        }
        if (data.PlayerList?.Count > 45) 
        {
            contentStringBuild.Append("and more ...");
        }

        return new StringConverted { data = contentStringBuild.ToString(), count = i };
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

    public static string GetServerData(ServerInfo data, ILogger logger)
    {
        var contentStringBuild = new StringBuilder();
        var upTime = new TimeSpan(0, 0, 30, (int) data.UpTime);
    
        contentStringBuild.Append($"IP: {data.ServerIp}");
        contentStringBuild.AppendLine();
        contentStringBuild.Append($"Runtime: {upTime}");

        return contentStringBuild.ToString();
    }

    private static string ResolveFactionKey(string factionKey = "")
    {
        return factionKey switch
        {
            "US" => ":flag_us:",
            // "RHS_US" => "RHS_:flag_us:",
            "USSR" => ":flag_ru:",
            // "RHS_RF_MSV" => "RHS_:flag_ru:", same for us than rus ...
            "FIA" => "<:FIA:1109836486536347800>",
            "BLUFOR" => "<:BLUFOR:1147157728435900416>",
            "OPFOR" => "<:OPFOR:1147157820354089062>",
            "INDFOR" => "<:INDFOR:1147157785344217140>",
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
