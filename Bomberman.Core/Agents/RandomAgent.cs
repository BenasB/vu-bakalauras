using System.Numerics;
using Bomberman.Core.Tiles;

namespace Bomberman.Core.Agents;

public class RandomAgent : IUpdatable
{
    public Vector2 Position => _player.Position;
    public bool Alive => _player.Alive;

    public List<GridPosition> CurrentPath { get; private set; }

    private readonly Player _player;
    private readonly TileMap _tileMap;

    private Direction _currentMovingDirection = Direction.None;
    private GridPosition _currentlyMovingTo;
    private BombTile? _bombTile;

    public RandomAgent(GridPosition startPosition, TileMap tileMap)
    {
        _tileMap = tileMap;
        _player = new Player(startPosition, tileMap);

        CurrentPath = FindBombPlacementPath(_player.Position.ToGridPosition());
        _currentlyMovingTo = CurrentPath[^1];
    }

    public void Update(TimeSpan deltaTime)
    {
        _player.Update(deltaTime);

        // TODO: turn this into a finite state machine (states: moving towards, moving away from bomb, waiting for bomb)
        if (!IsOnGridPosition(_player.Position, _currentlyMovingTo))
            return;

        var playerGridPosition = _player.Position.ToGridPosition();

        // Upon reaching a tile on the path,
        // adjust the current moving direction to point to the next tile in the path
        CurrentPath.RemoveAt(CurrentPath.Count - 1);

        if (CurrentPath.Count == 0) // Bomb tile reached
        {
            // TODO: Place bomb here
            CurrentPath = FindBombPlacementPath(playerGridPosition);
        }

        _currentlyMovingTo = CurrentPath[^1];

        if (_currentlyMovingTo.Row > playerGridPosition.Row)
            _currentMovingDirection = Direction.Down;
        else if (_currentlyMovingTo.Row < playerGridPosition.Row)
            _currentMovingDirection = Direction.Up;
        else if (_currentlyMovingTo.Column > playerGridPosition.Column)
            _currentMovingDirection = Direction.Right;
        else if (_currentlyMovingTo.Column < playerGridPosition.Column)
            _currentMovingDirection = Direction.Left;
        else
            _currentMovingDirection = Direction.None;

        _player.SetMovingDirection(_currentMovingDirection);
    }

    private static bool IsOnGridPosition(Vector2 position, GridPosition gridPosition)
    {
        const float threshold = 0.1f * Constants.TileSize;
        return Math.Abs(position.Y - gridPosition.Row * Constants.TileSize) <= threshold
            && Math.Abs(position.X - gridPosition.Column * Constants.TileSize) <= threshold;
    }

    /// <summary>
    /// Finds a (reversed) path to a tile where the agent should place a bomb
    /// </summary>
    private List<GridPosition> FindBombPlacementPath(GridPosition startingPosition)
    {
        var stack = new Stack<GridPosition>();

        // Position is not visited yet if it does not have a parent assigned
        var parents = new GridPosition?[_tileMap.Width, _tileMap.Length];

        var startingParent = new GridPosition(-1, -1);
        parents[startingPosition.Row, startingPosition.Column] = startingParent;
        stack.Push(startingPosition);

        var rnd = new Random();

        while (stack.Count != 0)
        {
            var position = stack.Pop();
            var neighbours = position
                .Neighbours.Where(neighbour => parents[neighbour.Row, neighbour.Column] == null)
                .ToArray();
            rnd.Shuffle(neighbours);

            foreach (var neighbour in neighbours)
            {
                parents[neighbour.Row, neighbour.Column] = position;

                var neighbourTile = _tileMap.GetTile(neighbour);

                if (neighbourTile is BoxTile && position != startingPosition)
                {
                    var parent = position;
                    var path = new List<GridPosition>();
                    while (parent != startingParent)
                    {
                        if (parent == null)
                            throw new InvalidOperationException(
                                "Encountered a null parent when gathering the bomb placement path"
                            );

                        path.Add(parent);
                        parent = parents[parent.Row, parent.Column];
                    }

                    // path.Reverse();

                    return path;
                }

                if (neighbourTile == null)
                    stack.Push(neighbour);
            }
        }

        // TODO: Maybe best to not blow themselves up?
        return [];
    }

    private GridPosition ChooseBombAvoidanceTile(BombTile bombTile, GridPosition currentPosition)
    {
        throw new NotImplementedException();
    }
}
