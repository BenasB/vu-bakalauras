using System.Numerics;
using Bomberman.Core.Tiles;
using Bomberman.Core.Utilities;

namespace Bomberman.Core;

public class Player : IUpdatable, IDamageable
{
    public Vector2 Position { get; private set; }

    private Vector2 _velocityDirection = Vector2.Zero;

    private const float Speed = Constants.TileSize * 3;

    public bool Alive { get; private set; } = true;

    private BombTile? _placedBombTile;
    private readonly TileMap _tileMap;

    public Player(GridPosition startPosition, TileMap tileMap)
    {
        _tileMap = tileMap;
        Position = startPosition;
    }

    internal Player(Player original, TileMap tileMap)
    {
        Position = original.Position;
        _velocityDirection = original._velocityDirection;
        Alive = original.Alive;
        _placedBombTile =
            original._placedBombTile == null
                ? null
                : tileMap.GetTile(original._placedBombTile.Position) as BombTile;
        // TODO: why this is always triggered in a simulation ?? throw new InvalidOperationException("Expected there to be a bomb");
        _tileMap = tileMap;
    }

    public void Update(TimeSpan deltaTime)
    {
        if (!Alive)
            return;

        if (_velocityDirection == Vector2.Zero)
            return;

        var velocity = _velocityDirection * Speed * (float)deltaTime.TotalSeconds;
        var adjustedVelocityLength = PhysicsManager.Raycast(_tileMap, Position, velocity);
        var adjustedVelocity = _velocityDirection * adjustedVelocityLength;

        var newPosition = Vector2.Add(Position, adjustedVelocity);

        Position = newPosition;
    }

    public void SetMovingDirection(Direction direction)
    {
        _velocityDirection = direction switch
        {
            Direction.None => Vector2.Zero,
            Direction.Down => Vector2.UnitY,
            Direction.Up => -Vector2.UnitY,
            Direction.Left => -Vector2.UnitX,
            Direction.Right => Vector2.UnitX,
            _ => throw new InvalidOperationException("Unexpected direction"),
        };
    }

    public BombTile PlaceBomb()
    {
        if (_placedBombTile is { Detonated: false })
            throw new InvalidOperationException("Player has already placed a bomb");

        var gridPosition = Position.ToGridPosition();
        var bombTile = new BombTile(gridPosition, _tileMap, 1);
        _tileMap.PlaceTile(bombTile);
        _placedBombTile = bombTile;

        return bombTile;
    }

    public void TakeDamage()
    {
        Alive = false;
    }
}
