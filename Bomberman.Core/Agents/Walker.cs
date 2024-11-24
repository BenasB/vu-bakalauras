using System.Numerics;
using Bomberman.Core.Utilities;

namespace Bomberman.Core.Agents;

internal class Walker
{
    private GridPosition? _currentlyMovingTo;
    private readonly List<GridPosition> _path;
    private readonly Player _player;

    public bool Finished => _currentlyMovingTo == null;
    public IReadOnlyList<GridPosition> Path { get; }

    public Walker(List<GridPosition> path, Player player)
    {
        _path = path;
        _player = player;
        _currentlyMovingTo = path[^1];
        Path = path;
    }

    internal Walker(Walker original, Player player)
    {
        _currentlyMovingTo =
            original._currentlyMovingTo == null ? null : original._currentlyMovingTo with { };
        _path = original._path.Select(gp => gp with { }).ToList();
        _player = player;
        Path = _path;
    }

    /// <returns>"Currently moving to" position if it changed</returns>
    public GridPosition? UpdatePlayerMovingDirection()
    {
        if (_currentlyMovingTo == null)
            return null;

        if (!IsOnGridPosition(_player.Position, _currentlyMovingTo))
            return null;

        var playerGridPosition = _player.Position.ToGridPosition();

        // Upon reaching a tile on the path,
        // adjust the current moving direction to point to the next tile in the path
        _path.RemoveAt(_path.Count - 1);

        if (_path.Count == 0) // End of path
        {
            _currentlyMovingTo = null;
            _player.SetMovingDirection(Direction.None);
            return null;
        }

        _currentlyMovingTo = _path[^1];

        if (_currentlyMovingTo.Row > playerGridPosition.Row)
            _player.SetMovingDirection(Direction.Down);
        else if (_currentlyMovingTo.Row < playerGridPosition.Row)
            _player.SetMovingDirection(Direction.Up);
        else if (_currentlyMovingTo.Column > playerGridPosition.Column)
            _player.SetMovingDirection(Direction.Right);
        else if (_currentlyMovingTo.Column < playerGridPosition.Column)
            _player.SetMovingDirection(Direction.Left);
        else
            _player.SetMovingDirection(Direction.None);

        return _currentlyMovingTo;
    }

    private static bool IsOnGridPosition(Vector2 position, GridPosition gridPosition)
    {
        const float threshold = 0.1f * Constants.TileSize;
        return Math.Abs(position.Y - gridPosition.Row * Constants.TileSize) <= threshold
            && Math.Abs(position.X - gridPosition.Column * Constants.TileSize) <= threshold;
    }
}
