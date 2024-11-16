using System.Numerics;
using Bomberman.Core.Tiles;
using Bomberman.Core.Utilities;

namespace Bomberman.Core;

public class Player(GridPosition startPosition, TileMap tileMap) : IUpdatable, IDamageable
{
    public Vector2 Position { get; private set; } = startPosition;

    private Vector2 _velocityDirection = Vector2.Zero;

    private const float Speed = Constants.TileSize * 3;

    public bool Alive { get; private set; } = true;

    private BombTile? _placedBombTile;

    public void Update(TimeSpan deltaTime)
    {
        if (!Alive)
            return;

        var newPosition = Vector2.Add(
            Position,
            _velocityDirection * Speed * (float)deltaTime.TotalSeconds
        );

        var snapVector = GetSnapOnMovementOppositeAxis(newPosition);
        newPosition = Vector2.Add(newPosition, snapVector);

        var collisionData = tileMap.IsColliding(newPosition, this);
        if (collisionData != null)
        {
            newPosition = Vector2.Subtract(newPosition, snapVector);
            newPosition = Vector2.Add(newPosition, collisionData.Value);
        }

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
        var bombTile = new BombTile(gridPosition, tileMap, 1);
        tileMap.PlaceTile(bombTile);
        _placedBombTile = bombTile;

        return bombTile;
    }

    private Vector2 GetSnapOnMovementOppositeAxis(Vector2 newPosition)
    {
        const double thresholdHorizontally = 0.25 * Constants.TileSize;
        const double thresholdVertically = 0.3 * Constants.TileSize;

        if (_velocityDirection.Y == 0 && _velocityDirection.X != 0) // Moving horizontally
        {
            var positionOnTile = newPosition.Y % Constants.TileSize;
            if (positionOnTile < Constants.TileSize / 2.0f) // Snap from above
            {
                var diff = positionOnTile;
                if (diff < thresholdHorizontally)
                    return Vector2.UnitY * -diff;
            }
            else // Snap from below
            {
                var diff = Constants.TileSize - positionOnTile;
                if (diff < thresholdHorizontally)
                    return Vector2.UnitY * diff;
            }
        }
        else if (_velocityDirection.X == 0 && _velocityDirection.Y != 0) // Moving vertically
        {
            var positionOnTile = newPosition.X % Constants.TileSize;
            if (positionOnTile < Constants.TileSize / 2.0f) // Snap from right
            {
                var diff = positionOnTile;
                if (diff < thresholdVertically)
                    return Vector2.UnitX * -diff;
            }
            else // Snap from left
            {
                var diff = Constants.TileSize - positionOnTile;
                if (diff < thresholdVertically)
                    return Vector2.UnitX * diff;
            }
        }

        return Vector2.Zero;
    }

    public void TakeDamage()
    {
        Alive = false;
    }
}
