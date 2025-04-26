using Bomberman.Core.Tiles;

namespace Bomberman.Core.Tests;

public class Pathfinding
{
    [Theory]
    [ClassData(typeof(PathfindingData))]
    public void FindsDistanceCorrectly(
        TileMap tileMap,
        GridPosition start,
        GridPosition end,
        int expectedDistance
    )
    {
        Assert.Equal(expectedDistance, tileMap.Distance(start, end));
    }

    [Theory]
    [ClassData(typeof(MaxPathfindingData))]
    public void FindsMaxDistanceCorrectly(TileMap tileMap, int expectedDistance)
    {
        Assert.Equal(expectedDistance, tileMap.MaxDistance());
    }
}

public class PathfindingData : TheoryData<TileMap, GridPosition, GridPosition, int>
{
    public PathfindingData()
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
            var expectedDistance = 12;
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
        }
    }
}

public class MaxPathfindingData : TheoryData<TileMap, int>
{
    public MaxPathfindingData()
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
