using Bomberman.Core.Tiles;

namespace Bomberman.Core;

public class Scenario
{
    public required TileMap TileMap { get; init; }
    public required GridPosition[] StartPositions { get; init; }

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

    public static Scenario SolidWall
    {
        get
        {
            var tileMap = new TileMap(17, 11).WithBorder();
            for (int i = 2; i < tileMap.Height - 2; i++)
            {
                tileMap.PlaceTile(new WallTile(new GridPosition(i, tileMap.Width - 3)));
            }

            return new Scenario
            {
                TileMap = tileMap,
                StartPositions =
                [
                    new GridPosition(Row: 5, Column: 6),
                    new GridPosition(Row: 5, Column: 15),
                ],
            };
        }
    }

    public static Scenario BacktrackSolidWall
    {
        get
        {
            var tileMap = new TileMap(17, 11).WithBorder();
            for (int i = 2; i < tileMap.Height - 1; i++)
            {
                tileMap.PlaceTile(new WallTile(new GridPosition(i, tileMap.Width - 3)));
            }

            for (int i = 2; i < tileMap.Width - 3; i++)
            {
                tileMap.PlaceTile(new WallTile(new GridPosition(2, i)));
            }

            return new Scenario
            {
                TileMap = tileMap,
                StartPositions =
                [
                    new GridPosition(Row: 5, Column: 6),
                    new GridPosition(Row: 5, Column: 15),
                ],
            };
        }
    }

    public static Scenario BoxWall
    {
        get
        {
            var tileMap = new TileMap(17, 11).WithBorder();
            for (int i = 1; i < tileMap.Height - 1; i++)
            {
                tileMap.PlaceTile(new BoxTile(new GridPosition(i, tileMap.Width - 3)));
            }

            return new Scenario
            {
                TileMap = tileMap,
                StartPositions =
                [
                    new GridPosition(Row: 5, Column: 6),
                    new GridPosition(Row: 5, Column: 15),
                ],
            };
        }
    }

    public static Scenario BacktrackBoxWall
    {
        get
        {
            var tileMap = new TileMap(17, 11).WithBorder();
            for (int i = 2; i < tileMap.Height - 1; i++)
            {
                tileMap.PlaceTile(new BoxTile(new GridPosition(i, tileMap.Width - 3)));
            }

            for (int i = 2; i < tileMap.Width - 3; i++)
            {
                tileMap.PlaceTile(new BoxTile(new GridPosition(2, i)));
            }

            return new Scenario
            {
                TileMap = tileMap,
                StartPositions =
                [
                    new GridPosition(Row: 5, Column: 6),
                    new GridPosition(Row: 5, Column: 15),
                ],
            };
        }
    }

    public static Scenario BoxWallShort
    {
        get
        {
            var tileMap = new TileMap(17, 11).WithBorder();
            tileMap.PlaceTile(new BoxTile(new GridPosition(tileMap.Height / 2, tileMap.Width - 3)));
            for (int i = 1; i < 3; i++)
            {
                tileMap.PlaceTile(
                    new BoxTile(new GridPosition(tileMap.Height / 2 + i, tileMap.Width - 3))
                );
                tileMap.PlaceTile(
                    new BoxTile(new GridPosition(tileMap.Height / 2 - i, tileMap.Width - 3))
                );
            }

            return new Scenario
            {
                TileMap = tileMap,
                StartPositions =
                [
                    new GridPosition(Row: 5, Column: 6),
                    new GridPosition(Row: 5, Column: 15),
                ],
            };
        }
    }

    public static Scenario BoxWallMultiple
    {
        get
        {
            var tileMap = new TileMap(17, 11).WithBorder();
            for (int column = tileMap.Width - 3; column > tileMap.Width / 2; column -= 2)
            for (int i = 1; i < tileMap.Height - 1; i++)
            {
                tileMap.PlaceTile(new BoxTile(new GridPosition(i, column)));
            }

            return new Scenario
            {
                TileMap = tileMap,
                StartPositions =
                [
                    new GridPosition(Row: 5, Column: 6),
                    new GridPosition(Row: 5, Column: 15),
                ],
            };
        }
    }
    
    public static Scenario BombInTheWay
    {
        get
        {
            var tileMap = new TileMap(15, 10).WithBorder();
            tileMap.PlaceTile(new BombTile(new GridPosition(1, 3), tileMap, 1));

            return new Scenario
            {
                TileMap = tileMap,
                StartPositions =
                [
                    new GridPosition(Row: 1, Column: 1),
                    new GridPosition(Row: 1, Column: tileMap.Width-2),
                ],
            };
        }
    }
}
