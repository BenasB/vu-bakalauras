using System.Numerics;
using Bomberman.Core.Utilities;

namespace Bomberman.Core.Agents;

internal class Walker : IUpdatable
{
    private const float TargetThreshold = 0.1f * Constants.TileSize;

    private readonly Player _player;
    private readonly Func<GridPosition?> _getNextTarget;
    private GridPosition? _target;
    private Vector2? _lastPosition;

    public bool IsFinished { get; private set; }
    public bool IsStuck { get; private set; }

    public Walker(Player player, Func<GridPosition?> getNextTarget)
    {
        _player = player;
        _target = null;
        _getNextTarget = getNextTarget;
        _lastPosition = null;
    }

    public Walker(Player player, Func<GridPosition?> getNextTarget, Walker original)
    {
        _player = player;
        _target = original._target;
        _getNextTarget = getNextTarget;
        _lastPosition = null;
        IsFinished = original.IsFinished;
        IsStuck = original.IsStuck;
    }

    public void Update(TimeSpan deltaTime)
    {
        if (IsFinished)
            return;

        if (_target != null)
        {
            if (_lastPosition != null && _lastPosition == _player.Position)
            {
                IsStuck = true;
                return;
            }

            IsStuck = false;
            _lastPosition = _player.Position;
            if (!_target.NearPosition(_player.Position, TargetThreshold))
                return;
        }

        do
        {
            _target = _getNextTarget.Invoke();

            if (_target != null)
                continue;

            _player.SetMovingDirection(Direction.None);
            IsFinished = true;
            return;
        } while (_target.NearPosition(_player.Position, TargetThreshold));

        if (_player.Position.Y < _target.Row * Constants.TileSize - TargetThreshold)
            _player.SetMovingDirection(Direction.Down);
        else if (_player.Position.Y > _target.Row * Constants.TileSize + TargetThreshold)
            _player.SetMovingDirection(Direction.Up);
        else if (_player.Position.X < _target.Column * Constants.TileSize - TargetThreshold)
            _player.SetMovingDirection(Direction.Right);
        else if (_player.Position.X > _target.Column * Constants.TileSize + TargetThreshold)
            _player.SetMovingDirection(Direction.Left);
        else
            throw new InvalidOperationException(
                "Could not determine movement direction from the target"
            );
    }
}
