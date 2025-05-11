using System.Numerics;
using Bomberman.Core.Utilities;

namespace Bomberman.Core.Agents;

internal class Walker : IUpdatable
{
    private const float TargetThreshold = 0.1f;

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

        _target = _getNextTarget.Invoke();

        if (_target == null)
        {
            _player.SetMovingDirection(Direction.None);
            IsFinished = true;
            return;
        }

        var playerPosition = _player.Position.ToGridPosition();
        if (playerPosition.Row < _target.Row)
            _player.SetMovingDirection(Direction.Down);
        else if (playerPosition.Row > _target.Row)
            _player.SetMovingDirection(Direction.Up);
        else if (playerPosition.Column < _target.Column)
            _player.SetMovingDirection(Direction.Right);
        else if (playerPosition.Column > _target.Column)
            _player.SetMovingDirection(Direction.Left);
    }
}
