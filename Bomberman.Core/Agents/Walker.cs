using System.Numerics;
using Bomberman.Core.Tiles;

namespace Bomberman.Core.Agents;

internal class Walker : IUpdatable
{
    public const float TargetThreshold = 0.1f * Constants.TileSize;

    private readonly Player _player;
    private readonly TileMap _tileMap;
    private readonly Func<GridPosition?> _getNextTarget;
    private GridPosition? _target;
    private Vector2? _lastPosition;

    public bool IsFinished { get; private set; }
    public bool IsStuck { get; private set; }
    private bool IsWaitingForExplosionToEnd { get; set; }

    public Walker(Player player, TileMap tileMap, Func<GridPosition?> getNextTarget)
    {
        _player = player;
        _tileMap = tileMap;
        _target = null;
        _getNextTarget = getNextTarget;
        _lastPosition = null;
    }

    public Walker(
        Player player,
        TileMap tileMap,
        Func<GridPosition?> getNextTarget,
        Walker original
    )
    {
        _player = player;
        _tileMap = tileMap;
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
            // Do not go into an explosion tile
            // Just wait for it to end
            if (_tileMap.GetTile(_target) is ExplosionTile)
            {
                IsWaitingForExplosionToEnd = true;
                _player.SetMovingDirection(Direction.None);
                return;
            }

            if (IsWaitingForExplosionToEnd)
            {
                IsWaitingForExplosionToEnd = false;
                SetMovingDirectionToTarget(_target);
            }

            if (_lastPosition != null && _lastPosition == _player.Position)
            {
                IsStuck = true;
                return;
            }

#if DEBUG
            if (_lastPosition != null)
            {
                var lastDiff =
                    Math.Abs(_lastPosition.Value.X - _target.Column * Constants.TileSize)
                    + Math.Abs(_lastPosition.Value.Y - _target.Row * Constants.TileSize);

                var currDiff =
                    Math.Abs(_player.Position.X - _target.Column * Constants.TileSize)
                    + Math.Abs(_player.Position.Y - _target.Row * Constants.TileSize);

                if (currDiff > lastDiff)
                    throw new InvalidOperationException(
                        "Distance increased, but we should be moving towards the target"
                    );
            }
#endif

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

        SetMovingDirectionToTarget(_target);
    }

    private void SetMovingDirectionToTarget(GridPosition target)
    {
        if (_player.Position.Y < target.Row * Constants.TileSize - TargetThreshold)
            _player.SetMovingDirection(Direction.Down);
        else if (_player.Position.Y > target.Row * Constants.TileSize + TargetThreshold)
            _player.SetMovingDirection(Direction.Up);
        else if (_player.Position.X < target.Column * Constants.TileSize - TargetThreshold)
            _player.SetMovingDirection(Direction.Right);
        else if (_player.Position.X > target.Column * Constants.TileSize + TargetThreshold)
            _player.SetMovingDirection(Direction.Left);
        else
            throw new InvalidOperationException(
                "Could not determine movement direction from the target"
            );
    }
}
