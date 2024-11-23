using System.Numerics;
using Bomberman.Core.Utilities;

namespace Bomberman.Core.Agents;

internal class Walker(List<GridPosition> path, Player player)
{
    private GridPosition? _currentlyMovingTo = path[^1];
    public bool Finished => _currentlyMovingTo == null;
    public IReadOnlyList<GridPosition> Path { get; } = path;

    /// <summary>
    ///
    /// </summary>
    /// <returns>Just changed moving to position.If player changed the grid position it's currently moving to</returns>
    public GridPosition? UpdatePlayerMovingDirection()
    {
        if (_currentlyMovingTo == null)
            return null;

        if (!IsOnGridPosition(player.Position, _currentlyMovingTo))
            return null;

        var playerGridPosition = player.Position.ToGridPosition();

        // Upon reaching a tile on the path,
        // adjust the current moving direction to point to the next tile in the path
        path.RemoveAt(path.Count - 1);

        if (path.Count == 0) // End of path
        {
            _currentlyMovingTo = null;
            player.SetMovingDirection(Direction.None);
            return null;
        }

        _currentlyMovingTo = path[^1];

        if (_currentlyMovingTo.Row > playerGridPosition.Row)
            player.SetMovingDirection(Direction.Down);
        else if (_currentlyMovingTo.Row < playerGridPosition.Row)
            player.SetMovingDirection(Direction.Up);
        else if (_currentlyMovingTo.Column > playerGridPosition.Column)
            player.SetMovingDirection(Direction.Right);
        else if (_currentlyMovingTo.Column < playerGridPosition.Column)
            player.SetMovingDirection(Direction.Left);
        else
            player.SetMovingDirection(Direction.None);

        return _currentlyMovingTo;
    }

    private static bool IsOnGridPosition(Vector2 position, GridPosition gridPosition)
    {
        const float threshold = 0.1f * Constants.TileSize;
        return Math.Abs(position.Y - gridPosition.Row * Constants.TileSize) <= threshold
            && Math.Abs(position.X - gridPosition.Column * Constants.TileSize) <= threshold;
    }
}
