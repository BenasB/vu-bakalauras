namespace Bomberman.Core;

public class Scenario
{
    public required TileMap TileMap { get; init; }
    public required GridPosition[] StartPositions { get; init; }

    public static Scenario Empty =>
        new()
        {
            TileMap = new TileMap(17, 11).WithBorder(),
            StartPositions =
            [
                new GridPosition(Row: 5, Column: 6),
                new GridPosition(Row: 5, Column: 15),
            ],
        };

    public static Scenario Default =>
        new()
        {
            TileMap = new TileMap(17, 11)
                .WithRandomTileFill()
                .WithBorder()
                .WithCheckerPattern()
                .WithSpaceAround(
                    new GridPosition(Row: 5, Column: 4),
                    new GridPosition(Row: 5, Column: 12)
                ),
            StartPositions =
            [
                new GridPosition(Row: 5, Column: 4),
                new GridPosition(Row: 5, Column: 12),
            ],
        };
}
