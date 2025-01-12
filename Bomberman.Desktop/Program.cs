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
}

using var game = new BombermanGame(options);
game.Run();
