using System.Numerics;
using Bomberman.Core.Tiles;
using Bomberman.Core.Utilities;

namespace Bomberman.Core;

public class Player : IUpdatable, IDamageable
{
    public Vector2 Position { get; private set; }

    public int BombRange { get; } = 1;

    public int MaxPlacedBombs { get; } = 1;

    public float Speed { get; } = 2;

    private Vector2 _velocityDirection = Vector2.Zero;

    public bool Alive { get; private set; } = true;

    internal bool CanPlaceBomb =>
        _placedBombTiles.Count < MaxPlacedBombs || _placedBombTiles.Any(bomb => bomb.Detonated);

    private readonly List<BombTile> _placedBombTiles = [];
    private readonly TileMap _tileMap;

    public Player(GridPosition startPosition, TileMap tileMap)
    {
        _tileMap = tileMap;
        Position = startPosition;
    }

    internal Player(Player original, TileMap tileMap)
    {
        Position = original.Position;
        BombRange = original.BombRange;
        MaxPlacedBombs = original.MaxPlacedBombs;
        Speed = original.Speed;
        _velocityDirection = original._velocityDirection;
        Alive = original.Alive;
        _placedBombTiles = original
            ._placedBombTiles.Where(bomb => !bomb.Detonated)
            .Select(activeBomb =>
                tileMap.GetTile(activeBomb.Position) as BombTile
                ?? throw new InvalidOperationException("Expected there to be a bomb")
            )
            .ToList();
        _tileMap = tileMap;
    }

    public void Update(TimeSpan deltaTime)
    {
        if (!Alive)
            return;

        if (_tileMap.GetTile(Position.ToGridPosition()) is IEnterable enterableTile)
        {
            enterableTile.OnEntered(this);
        }

        if (_velocityDirection == Vector2.Zero)
            return;

        var snappedPosition = Vector2.Add(Position, GetSnapOnMovementOppositeAxis(Position));

        var velocity =
            _velocityDirection * Speed * Constants.TileSize * (float)deltaTime.TotalSeconds;
        var adjustedVelocityLength = PhysicsManager.Raycast(
            this,
            _tileMap,
            snappedPosition,
            velocity
        );
        var adjustedVelocity = _velocityDirection * adjustedVelocityLength;

        if (adjustedVelocityLength != 0)
            Position = Vector2.Add(snappedPosition, adjustedVelocity);
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
        if (!CanPlaceBomb)
            throw new InvalidOperationException(
                "Player has already placed as much bombs as they can"
            );

        var gridPosition = Position.ToGridPosition();
        var bombTile = new BombTile(gridPosition, _tileMap, BombRange);
        _tileMap.PlaceTile(bombTile);

        if (_placedBombTiles.Count == MaxPlacedBombs)
            _placedBombTiles.RemoveAll(bomb => bomb.Detonated);

        if (_placedBombTiles.Count == MaxPlacedBombs)
            throw new InvalidOperationException(
                "Expected to clean up some detonated bombs from the list, but active bomb count did not decrease"
            );

        _placedBombTiles.Add(bombTile);

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
