using System.Text;
using discordPlayerList.Models.Request;

namespace discordPlayerList.Services;

public static class RabbitToDiscordConverter
{
    public static string GetPlayerList(ServerGameData data)
    {
        var contentStringBuild = new StringBuilder();

        data.PlayerList.ForEach(x =>
        {
            contentStringBuild.Append($"- {x.Name}");
            contentStringBuild.AppendLine();
        });

        return contentStringBuild.ToString();
    }
}