using Bomberman.Core.Tiles;

namespace Bomberman.Core.Tests;

public class Pathfinding
{
    internal const float WalkingSpeed = 2;

    [Theory]
    [ClassData(typeof(ShortestDistanceData))]
    public void FindsDistanceCorrectly(
        TileMap tileMap,
        GridPosition start,
        GridPosition end,
        double expectedDistance
    )
    {
        Assert.Equal(expectedDistance, tileMap.ShortestDistance(start, end, WalkingSpeed));
    }

    [Theory]
    [ClassData(typeof(MaxShortestDistanceData))]
    public void FindsMaxDistanceCorrectly(TileMap tileMap, double expectedDistance)
    {
        Assert.Equal(expectedDistance, tileMap.MaxShortestDistance(WalkingSpeed));
    }

    [Theory]
    [ClassData(typeof(ShortestPathData))]
    public void FindsPathCorrectly(
        TileMap tileMap,
        GridPosition start,
        GridPosition end,
        List<GridPosition>? expectedPath
    )
    {
        Assert.Equal(expectedPath, tileMap.ShortestPath(start, end, WalkingSpeed));
    }
}

public class ShortestDistanceData : TheoryData<TileMap, GridPosition, GridPosition, double>
{
    public ShortestDistanceData()
    {
        Add(new TileMap(5, 5), new GridPosition(0, 0), new GridPosition(0, 1), 1);
        Add(new TileMap(5, 5), new GridPosition(0, 0), new GridPosition(1, 0), 1);
        Add(new TileMap(5, 5), new GridPosition(0, 0), new GridPosition(0, 0), 0);
        Add(new TileMap(5, 5), new GridPosition(0, 0), new GridPosition(4, 4), 8);

        // S 0 1 0 0
        // 0 0 1 0 0
        // 0 0 1 0 0
        // 0 0 1 0 0
        // 0 0 1 0 F
        {
            var tileMap = new TileMap(5, 5);
            for (int i = 0; i < tileMap.Height; i++)
            {
                tileMap.PlaceTile(new WallTile(new GridPosition(i, 2)));
            }

            Add(tileMap, new GridPosition(0, 0), new GridPosition(4, 4), -1);
        }

        // S 0 1 0 F
        // 0 0 1 0 0
        // 0 0 1 0 0
        // 0 0 1 0 0
        // 0 0 0 0 0
        {
            var tileMap = new TileMap(5, 5);
            for (int i = 0; i < tileMap.Height - 1; i++)
            {
                tileMap.PlaceTile(new WallTile(new GridPosition(i, 2)));
            }
            Add(tileMap, new GridPosition(0, 0), new GridPosition(0, 4), 12);
        }

        // S 0 1 0 F
        // 0 0 1 0 0
        // 0 0 1 0 0
        // 0 0 1 0 0
        // 0 0 ? 0 0
        {
            var start = new GridPosition(0, 0);
            var finish = new GridPosition(0, 4);
            var expectedDistance = 12.0;
            var height = 5;
            var width = 5;
            var gapPosition = new GridPosition(height - 1, width / 2);

            TileMap GetTileMap()
            {
                var tileMap = new TileMap(width, height);
                for (int i = 0; i < tileMap.Height - 1; i++)
                {
                    tileMap.PlaceTile(new WallTile(new GridPosition(i, 2)));
                }

                return tileMap;
            }

            {
                var tileMap = GetTileMap();
                tileMap.PlaceTile(new BombTile(gapPosition, tileMap, 1));
                Add(tileMap, start, finish, expectedDistance);
            }

            {
                var tileMap = GetTileMap();
                tileMap.PlaceTile(new ExplosionTile(gapPosition, tileMap, TimeSpan.MaxValue));
                Add(tileMap, start, finish, expectedDistance);
            }

            {
                var tileMap = GetTileMap();
                tileMap.PlaceTile(new BoxTile(gapPosition));
                var addedDistanceDueToBox =
                    (BombTile.DetonateAfter + BombTile.ExplosionDuration).TotalSeconds
                    * Pathfinding.WalkingSpeed;
                Add(tileMap, start, finish, expectedDistance + addedDistanceDueToBox);
            }
        }
    }
}

public class MaxShortestDistanceData : TheoryData<TileMap, double>
{
    public MaxShortestDistanceData()
    {
        // 0 0 0 0 0
        // 0 0 0 0 0
        // 0 0 0 0 0
        // 0 0 0 0 0
        // 0 0 0 0 0
        Add(new TileMap(5, 5), 8);

        // 0 0 1 0 0
        // 0 0 1 0 0
        // 0 0 1 0 0
        // 0 0 1 0 0
        // 0 0 1 0 0
        {
            var tileMap = new TileMap(5, 5);
            for (int i = 0; i < tileMap.Height; i++)
            {
                tileMap.PlaceTile(new WallTile(new GridPosition(i, 2)));
            }

            Add(tileMap, 5);
        }

        // 0 0 1 0 0
        // 0 0 1 0 0
        // 0 0 1 0 0
        // 0 0 1 0 0
        // 0 0 0 0 0
        {
            var tileMap = new TileMap(5, 5);
            for (int i = 0; i < tileMap.Height - 1; i++)
            {
                tileMap.PlaceTile(new WallTile(new GridPosition(i, 2)));
            }
            Add(tileMap, 12);
        }

        // 0 0 0 0 0
        // 0 1 1 1 0
        // 0 0 0 1 0
        // 0 0 0 1 0
        // 0 0 0 1 0
        {
            var tileMap = new TileMap(5, 5);
            for (int i = 1; i < tileMap.Height; i++)
            {
                tileMap.PlaceTile(new WallTile(new GridPosition(i, 3)));
            }
            for (int i = 1; i < tileMap.Width - 2; i++)
            {
                tileMap.PlaceTile(new WallTile(new GridPosition(1, i)));
            }
            Add(tileMap, 14);
        }
    }
}

public class ShortestPathData : TheoryData<TileMap, GridPosition, GridPosition, List<GridPosition>?>
{
    public ShortestPathData()
    {
        // S 0 0 0 F
        Add(
            new TileMap(5, 1),
            new GridPosition(0, 0),
            new GridPosition(0, 4),
            new List<GridPosition> { new(0, 0), new(0, 1), new(0, 2), new(0, 3), new(0, 4) }
        );

        // S 0 1 0 F
        {
            var tileMap = new TileMap(5, 1);
            tileMap.PlaceTile(new WallTile(new GridPosition(0, 2)));
            Add(tileMap, new GridPosition(0, 0), new GridPosition(0, 4), null);
        }

        // S 0 1 0 0
        // 0 0 1 0 0
        // 0 0 1 0 0
        // 0 0 1 0 0
        // 0 0 0 0 F
        {
            var tileMap = new TileMap(5, 5);
            for (int i = 0; i < tileMap.Height - 1; i++)
            {
                tileMap.PlaceTile(new WallTile(new GridPosition(i, 2)));
            }

            Add(
                tileMap,
                new GridPosition(0, 0),
                new GridPosition(4, 4),
                new List<GridPosition>
                {
                    new(0, 0),
                    new(1, 0),
                    new(1, 1),
                    new(2, 1),
                    new(3, 1),
                    new(4, 1),
                    new(4, 2),
                    new(4, 3),
                    new(4, 4),
                }
            );
        }

        // S 0 1 0 0
        // 0 0 1 0 0
        // 0 0 1 0 0
        // 0 0 1 0 0
        // 0 0 1 0 F
        {
            var tileMap = new TileMap(5, 5);
            for (int i = 0; i < tileMap.Height; i++)
            {
                tileMap.PlaceTile(new WallTile(new GridPosition(i, 2)));
            }

            Add(tileMap, new GridPosition(0, 0), new GridPosition(4, 4), null);
        }
    }
}
