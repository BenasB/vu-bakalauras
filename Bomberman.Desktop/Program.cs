using System;
using Bomberman.Desktop;

var options = new BombermanGameOptions();

const string flagPrefix = "--";
for (int i = 0; i < args.Length; i++)
{
    string flag;
    if (args[i].StartsWith(flagPrefix))
        flag = args[i][flagPrefix.Length..].ToLower();
    else
        throw new InvalidOperationException($"Unsupported flag '{args[i]}'");

    const string playerPrefix = "player";
    if (flag.StartsWith(playerPrefix))
    {
        var number = flag[playerPrefix.Length..];
        i++;
        var value = args[i];
        if (Enum.TryParse<PlayerType>(value, ignoreCase: true, out var parsedValue))
        {
            switch (number)
            {
                case "one":
                    options.PlayerOne = parsedValue;
                    break;
                case "two":
                    options.PlayerTwo = parsedValue;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported player number '{number}'");
            }
        }
        else
            throw new InvalidOperationException($"Unsupported '{flag}' value '{value}'");
    }
    else if (flag == "export")
    {
        if (options.PlayerOne != PlayerType.Mcts && options.PlayerTwo != PlayerType.Mcts)
            throw new InvalidOperationException(
                $"Flag 'export' may only be used with at least one '{nameof(PlayerType.Mcts)}' agent"
            );

        options.Export = true;
    }
    else
    {
        throw new InvalidOperationException($"Unsupported flag '{args[i]}'");
    }
}

using var game = new BombermanGame(options);
game.Run();
