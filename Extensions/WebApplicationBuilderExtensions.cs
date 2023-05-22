namespace DiscordPlayerList.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder UseEnvironment(this WebApplicationBuilder builder)
    {       
        var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
        if (File.Exists(file))
        {
            AddEnvironmentVariable(file);
        }
        
        var localEnvFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env.local");
        if (File.Exists(localEnvFile))
        {
            AddEnvironmentVariable(localEnvFile, overrideExisting: true);
        }
        
        return builder;
    }
    
    private static void AddEnvironmentVariable(string filePath, bool overrideExisting = false)
    {
        var content = File.ReadLines(filePath);
        foreach (var line in content)
        {
            var comment = line.StartsWith("#");
            if (comment)
            {
                continue;
            }

            if (string.IsNullOrEmpty(line))
            {
                continue;
            }
            
            var split = line.Split('=');
            if (split.Length < 2)
            {
                throw new InvalidOperationException($"Invalid environment file line {line}");
            }

            var key = split[0];
            var value = split[1];

            if (value.StartsWith('\'') && value.EndsWith('\''))
            {
                value = value.Remove(0, 1);
                value = value.Remove(value.Length - 1, 1);
            }

            var existing = Environment.GetEnvironmentVariable(key);
            if (existing is not null && overrideExisting == false)
            {
                continue;
            }
            
            Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
        }
    }
}