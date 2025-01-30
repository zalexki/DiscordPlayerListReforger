// using System.Collections.Generic;
// using DiscordPlayerListShared.Models.Request;
// using SixLabors.ImageSharp;
// using TableToImageExport;

// namespace DiscordPlayerListConsumer.Services.Helpers;

// public static class TableToImageHelper
// {
//     // generate an array of random PlayerInfo objects  
//     public static List<PlayerInfo> GenerateRandomPlayerInfo(int count)
//     {
//         var players = new List<PlayerInfo>();
//         var random = new Random();
//         for (var i = 0; i < count; i++)
//         {
//             players.Add(new PlayerInfo
//             {
//                 Name = "Player " + i,
//                 Kills = random.Next(0, 100),
//                 Deaths = random.Next(0, 100),
//                 Platform = "PC",
//                 Faction = "US",
//                 FriendlyAiKills = random.Next(0, 100),
//                 FriendlyPlayerKills = random.Next(0, 100),
//                 KillsAi = random.Next(0, 100)
//             });
//         }
//         return players;
//     }
    
    
//     public static bool GenerateImageTable()
//     {
//         var generator = new TableGenerator();
//         var players = GenerateRandomPlayerInfo(10);
//         generator.LoadFromObjects<PlayerInfo>(players, "Platform.Name.Kills.Deaths.FriendlyAiKills", new Vector2I(0));
//         generator.ExpandRowsToContent();
//         generator.ExpandColumnsToContent(10);
            
//         var bitmap = generator.ExportTableToImage();
//         try
//         {
//             bitmap.Save("img/test.jpg");
//         }
//         catch (Exception e)
//         {
//             Console.WriteLine(e);
//             return false;

    //     return bitmap;
    // }

    // generate an image from html table
    // public static Image GenerateTable(string html)
    // {
    //     var generator = new TableGenerator();
    //     generator.LoadFromHtml(html);
    //     var bitmap = generator.ExportTableToImage();

    //     return bitmap;
    // }

    // // generate an html table from a list of PlayerInfo objects 
    // public static string GenerateTable(List<PlayerInfo> players)
    //  {
    //      var generator = new TableGenerator();
    //      generator.LoadFromObjects<PlayerInfo>(players, x => x.Kills);
    //      return generator.ExportTableToHtml();
    //  }
// }
