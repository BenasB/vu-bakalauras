using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bomberman.Core.Agents;
using Bomberman.Core.Agents.MCTS;
using Bomberman.Core.Utilities;
using Bomberman.Desktop;

var options = new BombermanGameOptions();
var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
};

const string flagPrefix = "--";
for (int i = 0; i < args.Length; i++)
{
    string flag;
    if (args[i].StartsWith(flagPrefix))
        flag = args[i][flagPrefix.Length..].ToLower();
    else
        throw new InvalidOperationException(
            $"Flag '{args[i]}' does not start with the {flagPrefix} prefix"
        );

    const string playerPrefix = "player";
    if (flag.StartsWith(playerPrefix))
    {
        var numberString = flag[playerPrefix.Length..];

        var playerNumber = numberString switch
        {
            "one" => 1,
            "two" => 2,
            _ => throw new InvalidOperationException($"Unsupported player number '{numberString}'"),
        };

        i++;
        var value = args[i];
        if (!Enum.TryParse<AgentType>(value, ignoreCase: true, out var parsedValue))
        {
            throw new InvalidOperationException($"Unsupported '{flag}' value '{value}'");
        }

        MctsAgentOptions? mctsOptions = null;
        try
        {
            var maybeMctsOptions = args[i + 1];
            mctsOptions = JsonSerializer.Deserialize<MctsAgentOptions>(
                maybeMctsOptions,
                jsonOptions
            );
            i++;
        }
        catch (Exception) { }

        if (playerNumber == 1)
        {
            options.PlayerOne = parsedValue;
            if (mctsOptions != null)
                options.PlayerOneMctsOptions = mctsOptions;
        }
        else if (playerNumber == 2)
        {
            options.PlayerTwo = parsedValue;
            if (mctsOptions != null)
                options.PlayerTwoMctsOptions = mctsOptions;
        }
    }
    else if (flag == "seed")
    {
        i++;
        var value = args[i];
        if (int.TryParse(value, out var seed))
            options.Seed = seed;
        else
            throw new InvalidOperationException(
                $"Could not parse '{value}' into an integer to use as the seed"
            );
    }
    else if (flag == "report")
    {
        i++;
        var value = args[i];
        options.JsonReportFilePath = value;
    }
    else
    {
        throw new InvalidOperationException($"Unsupported flag '{flag}'");
    }
}

Logger.Information($"Starting game with the following options: {options}");
using var game = new BombermanGame(options);
game.Run();
