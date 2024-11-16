using System.Numerics;
using Bomberman.Core.Utilities;

namespace Bomberman.Core.Agents;

internal class Walker(List<GridPosition> path, Player player)
{
    // TODO: What if path is [] ?
    private GridPosition _currentlyMovingTo = path[^1];
    public bool Finished { get; private set; }
    public List<GridPosition> Path { get; } = path;

    public void UpdatePlayerMovingDirection()
    {
        if (Finished)
            return;

        if (!IsOnGridPosition(player.Position, _currentlyMovingTo))
            return;

        var playerGridPosition = player.Position.ToGridPosition();

        // Upon reaching a tile on the path,
        // adjust the current moving direction to point to the next tile in the path
        Path.RemoveAt(Path.Count - 1);

        if (Path.Count == 0) // End of path
        {
            Finished = true;
            player.SetMovingDirection(Direction.None);
            return;
        }

        _currentlyMovingTo = Path[^1];

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
    }

    private static bool IsOnGridPosition(Vector2 position, GridPosition gridPosition)
    {
        const float threshold = 0.1f * Constants.TileSize;
        return Math.Abs(position.Y - gridPosition.Row * Constants.TileSize) <= threshold
            && Math.Abs(position.X - gridPosition.Column * Constants.TileSize) <= threshold;
    }
}
