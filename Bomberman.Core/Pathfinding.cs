using Bomberman.Core.Tiles;

namespace Bomberman.Core;

internal static class Pathfinding
{
    internal static int Distance(this TileMap tileMap, GridPosition start, GridPosition finish)
    {
        var pq = new PriorityQueue<GridPosition, int>();
        pq.Enqueue(start, 0);

        var costs = new int[tileMap.Height, tileMap.Width];
        for (int row = 0; row < tileMap.Height; row++)
        for (int column = 0; column < tileMap.Width; column++)
        {
            costs[row, column] = int.MaxValue;
        }
        costs[start.Row, start.Column] = 0;

        while (pq.Count > 0)
        {
            var current = pq.Dequeue();

            if (current == finish)
                return costs[finish.Row, finish.Column];

            foreach (var neighbour in current.Neighbours)
            {
                if (
                    neighbour.Row < 0
                    || neighbour.Row >= tileMap.Height
                    || neighbour.Column < 0
                    || neighbour.Column >= tileMap.Width
                )
                    continue;

                var neighbourTile = tileMap.GetTile(neighbour);

                // TODO: Now BoxTile seems indestructible, handle it separately
                if (neighbourTile is WallTile or BoxTile)
                    continue;

                var newNeighbourCost = costs[current.Row, current.Column] + 1;

                if (newNeighbourCost >= costs[neighbour.Row, neighbour.Column])
                    continue;

                costs[neighbour.Row, neighbour.Column] = newNeighbourCost;
                var heuristic = neighbour.ManhattanDistance(finish);
                pq.Enqueue(neighbour, newNeighbourCost + heuristic);
            }
        }

        return -1;
    }

    internal static int MaxDistance(this TileMap tileMap)
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

        var (_, nextStart) = tileMap.MaxDistance(start);
        var (maxDistance, _) = tileMap.MaxDistance(nextStart);

        return maxDistance;
    }

    private static (int, GridPosition) MaxDistance(this TileMap tileMap, GridPosition start)
    {
        var pq = new PriorityQueue<GridPosition, int>();
        pq.Enqueue(start, 0);

        var costs = new int[tileMap.Height, tileMap.Width];
        for (int row = 0; row < tileMap.Height; row++)
        for (int column = 0; column < tileMap.Width; column++)
        {
            costs[row, column] = int.MaxValue;
        }
        costs[start.Row, start.Column] = 0;

        while (pq.Count > 0)
        {
            var current = pq.Dequeue();

            foreach (var neighbour in current.Neighbours)
            {
                if (
                    neighbour.Row < 0
                    || neighbour.Row >= tileMap.Height
                    || neighbour.Column < 0
                    || neighbour.Column >= tileMap.Width
                )
                    continue;

                var neighbourTile = tileMap.GetTile(neighbour);

                // TODO: Now BoxTile seems indestructible, handle it separately
                if (neighbourTile is WallTile or BoxTile)
                    continue;

                var newNeighbourCost = costs[current.Row, current.Column] + 1;

                if (newNeighbourCost >= costs[neighbour.Row, neighbour.Column])
                    continue;

                costs[neighbour.Row, neighbour.Column] = newNeighbourCost;
                pq.Enqueue(neighbour, newNeighbourCost);
            }
        }

        var max = -1;
        var maxRow = -1;
        var maxColumn = -1;
        for (int row = 0; row < tileMap.Height; row++)
        for (int column = 0; column < tileMap.Width; column++)
        {
            if (costs[row, column] == int.MaxValue)
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
}
