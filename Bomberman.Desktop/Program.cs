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

    if (flag == "player")
    {
        i++;
        var value = args[i];
        if (Enum.TryParse<GamePlayer>(value, ignoreCase: true, out var parsedValue))
            options.Player = parsedValue;
        else
            throw new InvalidOperationException($"Unsupported '{flag}' value '{value}'");
    }
    else if (flag == "export")
    {
        if (options.Player != GamePlayer.Agent)
            throw new InvalidOperationException(
                "Flag 'export' may only be used after setting the flag 'player' to 'agent'"
            );

        options.Export = true;
    }
}

using var game = new BombermanGame(options);
game.Run();
