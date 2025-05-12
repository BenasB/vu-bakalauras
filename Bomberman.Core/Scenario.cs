using Bomberman.Core.Tiles;

namespace Bomberman.Core;

public class Scenario
{
    public required TileMap TileMap { get; init; }
    public required GridPosition[] StartPositions { get; init; }
}

public class ScenarioFactory
{
    private readonly int _seed;

    public ScenarioFactory(int seed)
    {
        _seed = seed;
    }

    public Scenario Default
    {
        get
        {
            const int size = 13;
            var tileMap = new TileMap(size, size, _seed)
                .WithRandomTileFill()
                .WithBorder()
                .WithCheckerPattern()
                .WithSpaceAround(
                    new GridPosition(Row: 1, Column: 1),
                    new GridPosition(Row: size - 2, Column: size - 2)
                );

            return new Scenario
            {
                TileMap = tileMap,
                StartPositions =
                [
                    new GridPosition(Row: 1, Column: 1),
                    new GridPosition(Row: size - 2, Column: size - 2),
                ],
            };
        }
    }

    public Scenario Empty =>
        new()
        {
            TileMap = new TileMap(17, 11, _seed).WithBorder(),
            StartPositions =
            [
                new GridPosition(Row: 5, Column: 6),
                new GridPosition(Row: 5, Column: 15),
            ],
        };

    public Scenario SolidWall
    {
        get
        {
            var tileMap = new TileMap(17, 11, _seed).WithBorder();
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

    public Scenario BacktrackSolidWall
    {
        get
        {
            var tileMap = new TileMap(17, 11, _seed).WithBorder();
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

    public Scenario BoxWall
    {
        get
        {
            var tileMap = new TileMap(17, 11, _seed).WithBorder();
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

    public Scenario BacktrackBoxWall
    {
        get
        {
            var tileMap = new TileMap(17, 11, _seed).WithBorder();
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

    public Scenario BoxWallShort
    {
        get
        {
            var tileMap = new TileMap(17, 11, _seed).WithBorder();
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

    public Scenario BoxWallMultiple
    {
        get
        {
            var tileMap = new TileMap(17, 11, _seed).WithBorder();
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

    public Scenario BombInTheWay
    {
        get
        {
            var tileMap = new TileMap(15, 10, _seed).WithBorder();
            tileMap.PlaceTile(new BombTile(new GridPosition(1, 3), tileMap, 1));

            return new Scenario
            {
                TileMap = tileMap,
                StartPositions =
                [
                    new GridPosition(Row: 1, Column: 1),
                    new GridPosition(Row: 1, Column: tileMap.Width - 2),
                ],
            };
        }
    }
}
