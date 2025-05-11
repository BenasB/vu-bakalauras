using Bomberman.Core.Tiles;

namespace Bomberman.Core;

internal static class Pathfinding
{
    internal static double ShortestDistance(
        this TileMap tileMap,
        GridPosition start,
        GridPosition finish,
        float walkingSpeed
    )
    {
        var (costs, _) = tileMap.CalculateCosts(start, walkingSpeed, finish);
        var finishCost = costs[finish.Row, finish.Column];

        if (double.IsPositiveInfinity(finishCost))
            return -1;

        return finishCost;
    }

    internal static double MaxShortestDistance(this TileMap tileMap, float walkingSpeed)
    {
        GridPosition? start = null;
        for (int row = 0; row < tileMap.Height && start == null; row++)
        for (int column = 0; column < tileMap.Width && start == null; column++)
        {
            var pos = new GridPosition(row, column);
            if (tileMap.GetTile(pos) is not WallTile)
            {
                start = pos;
            }
        }

        if (start is null)
            throw new InvalidOperationException(
                "Could not find a tile to start the calculation from"
            );

        var (_, nextStart) = tileMap.MaxShortestDistance(start, walkingSpeed);
        var (maxDistance, _) = tileMap.MaxShortestDistance(nextStart, walkingSpeed);

        return maxDistance;
    }

    private static (double, GridPosition) MaxShortestDistance(
        this TileMap tileMap,
        GridPosition start,
        float walkingSpeed
    )
    {
        var (costs, _) = tileMap.CalculateCosts(start, walkingSpeed);

        var max = -1.0;
        var maxRow = -1;
        var maxColumn = -1;
        for (int row = 0; row < tileMap.Height; row++)
        for (int column = 0; column < tileMap.Width; column++)
        {
            if (double.IsPositiveInfinity(costs[row, column]))
                continue;

            if (costs[row, column] > max)
            {
                max = costs[row, column];
                maxRow = row;
                maxColumn = column;
            }
        }
        return (max, new GridPosition(maxRow, maxColumn));
    }

    internal static List<GridPosition>? ShortestPath(
        this TileMap tileMap,
        GridPosition start,
        GridPosition finish,
        float walkingSpeed
    )
    {
        if (start == finish)
        {
            return new List<GridPosition> { start };
        }

        var (_, parents) = tileMap.CalculateCosts(start, walkingSpeed, finish);

        var parent = parents[finish.Row, finish.Column];
        if (parent == null)
        {
            return null;
        }

        var path = new List<GridPosition> { finish };
        while (parent != null)
        {
            path.Add(parent);
            parent = parents[parent.Row, parent.Column];
        }

        path.Reverse();
        return path;
    }

    private static (double[,], GridPosition?[,]) CalculateCosts(
        this TileMap tileMap,
        GridPosition start,
        float walkingSpeed,
        GridPosition? finish = null
    )
    {
        var pq = new PriorityQueue<GridPosition, double>();
        pq.Enqueue(start, 0);

        var costs = new double[tileMap.Height, tileMap.Width];
        var parents = new GridPosition?[tileMap.Height, tileMap.Width];
        for (int row = 0; row < tileMap.Height; row++)
        for (int column = 0; column < tileMap.Width; column++)
        {
            costs[row, column] = double.PositiveInfinity;
        }
        costs[start.Row, start.Column] = 0;

        var distanceWalkedPerBoxRemovalTime =
            (BombTile.DetonateAfter + BombTile.ExplosionDuration).TotalSeconds * walkingSpeed;

        while (pq.Count > 0)
        {
            var current = pq.Dequeue();

            if (finish != null && current == finish)
                return (costs, parents);

            foreach (var neighbour in current.Neighbours)
            {
                if (!tileMap.IsPositionInsideBounds(neighbour))
                    continue;

                var neighbourTile = tileMap.GetTile(neighbour);

                if (neighbourTile is WallTile)
                    continue;

                var newNeighbourCost = neighbourTile switch
                {
                    BoxTile => costs[current.Row, current.Column]
                        + 1
                        + distanceWalkedPerBoxRemovalTime,
                    _ => costs[current.Row, current.Column] + 1,
                };

                if (newNeighbourCost >= costs[neighbour.Row, neighbour.Column])
                    continue;

                parents[neighbour.Row, neighbour.Column] = current;
                costs[neighbour.Row, neighbour.Column] = newNeighbourCost;
                pq.Enqueue(neighbour, newNeighbourCost);
            }
        }

        return (costs, parents);
    }
}
