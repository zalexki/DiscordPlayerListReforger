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
                // <:steam:1107786853874159737>
                // <:xbox:1107786791999787068>
                var emojiIconPlatform = x.Platform == "STEAM" ? "<:steam:1107786853874159737>" : "<:xbox:1107786791999787068>"; 
                contentStringBuild.Append($"{emojiIconPlatform} {x.Name}");
                contentStringBuild.AppendLine();
            });
        }

        return contentStringBuild.ToString();
    }

    public static string GetMissionData(ServerGameData data)
    {
        var contentStringBuild = new StringBuilder();
        contentStringBuild.Append($"name: {data.MissionName}");
        
        return contentStringBuild.ToString();
    }
}