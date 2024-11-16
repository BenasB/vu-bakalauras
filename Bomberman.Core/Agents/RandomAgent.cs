using System.Numerics;
using Bomberman.Core.Tiles;
using Bomberman.Core.Utilities;

namespace Bomberman.Core.Agents;

public class RandomAgent : IUpdatable
{
    public Vector2 Position => _player.Position;
    public bool Alive => _player.Alive;

    private readonly Player _player;
    private readonly TileMap _tileMap;

    public List<GridPosition>? CurrentPath { get; private set; }
    private GridPosition? _currentlyMovingTo;
    private BombTile? _bombTile;

    private readonly FiniteStateMachine<State> _stateMachine =
        new(
            State.GoingToPlaceBomb,
            fromTo =>
                fromTo switch
                {
                    (State.GoingToPlaceBomb, State.MovingAwayFromBomb) => true,
                    (State.MovingAwayFromBomb, State.WaitingForBomb) => true,
                    (State.WaitingForBomb, State.GoingToPlaceBomb) => true,
                    _ => false,
                }
        );

    private enum State
    {
        GoingToPlaceBomb, // Associated data: Path
        MovingAwayFromBomb, // Associated data: Path, BombTile
        WaitingForBomb, // Associated data: BombTile
    }

    public RandomAgent(GridPosition startPosition, TileMap tileMap)
    {
        _tileMap = tileMap;
        _player = new Player(startPosition, tileMap);

        // Initialize data needed for State.GoingToPlaceBomb
        CurrentPath = FindBombPlacementPath(_player.Position.ToGridPosition());
    }

    public void Update(TimeSpan deltaTime)
    {
        _player.Update(deltaTime);

        Action stateAction = _stateMachine.State switch
        {
            State.GoingToPlaceBomb => GoToPlaceBomb,
            State.MovingAwayFromBomb => GoToAvoidBomb,
            State.WaitingForBomb => WaitForBombDetonation,
            _ => throw new InvalidOperationException(
                "There is not associated action with this state"
            ),
        };

        stateAction.Invoke();
    }

    // Associated state: _currentlyMovingTo, CurrentPath
    private void GoToPlaceBomb()
    {
        if (CurrentPath == null)
        {
            var newPath = FindBombPlacementPath(_player.Position.ToGridPosition());
            if (newPath.Count == 0)
                CurrentPath = newPath;
        }

        CurrentPath ??= FindBombPlacementPath(_player.Position.ToGridPosition());
        var stillWalking = WalkThroughCurrentPath();

        if (stillWalking)
            return;

        _bombTile = _player.PlaceBomb();
        _stateMachine.Transition(State.MovingAwayFromBomb);
    }

    // Associated state: _currentlyMovingTo, CurrentPath
    private void GoToAvoidBomb()
    {
        if (_bombTile == null)
            throw new InvalidOperationException("There is no bomb to avoid");

        CurrentPath ??= FindBombAvoidancePath(_player.Position.ToGridPosition(), _bombTile);
        var stillWalking = WalkThroughCurrentPath();

        if (stillWalking)
            return;

        _stateMachine.Transition(State.WaitingForBomb);
    }

    private void WaitForBombDetonation()
    {
        if (_bombTile == null)
            throw new InvalidOperationException("There is no bomb to wait for");

        if (!_bombTile.Exploded)
            return;

        _bombTile = null;

        _stateMachine.Transition(State.GoingToPlaceBomb);
    }

    /// <returns><see langword="true"/> if the agent is still walking through path, otherwise <see langword="false"/></returns>
    private bool WalkThroughCurrentPath()
    {
        // TODO: what if CurrentPath is []

        if (CurrentPath == null)
            throw new InvalidOperationException("Trying to walk, but there is no path given");

        _currentlyMovingTo ??= CurrentPath[^1];

        if (!IsOnGridPosition(_player.Position, _currentlyMovingTo))
            return true;

        var playerGridPosition = _player.Position.ToGridPosition();

        // Upon reaching a tile on the path,
        // adjust the current moving direction to point to the next tile in the path
        CurrentPath.RemoveAt(CurrentPath.Count - 1);

        if (CurrentPath.Count == 0) // End of path
        {
            _currentlyMovingTo = null;
            CurrentPath = null;
            _player.SetMovingDirection(Direction.None);
            return false;
        }

        _currentlyMovingTo = CurrentPath[^1];

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

        return true;
    }

    private static bool IsOnGridPosition(Vector2 position, GridPosition gridPosition)
    {
        const float threshold = 0.1f * Constants.TileSize;
        return Math.Abs(position.Y - gridPosition.Row * Constants.TileSize) <= threshold
            && Math.Abs(position.X - gridPosition.Column * Constants.TileSize) <= threshold;
    }

    /// <summary>
    /// Finds a path to a tile where the agent should place a bomb
    /// </summary>
    private List<GridPosition> FindBombPlacementPath(GridPosition startingPosition)
    {
        var stack = new Stack<GridPosition>();
        var rnd = new Random();

        // Position is not visited yet if it does not have a parent assigned
        var parents = new GridPosition?[_tileMap.Width, _tileMap.Length];

        stack.Push(startingPosition);
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

                // If we encountered a neighbour box tile, then our position is our destination
                if (neighbourTile is BoxTile && position != startingPosition)
                    return CollectPath(parents, position, startingPosition);

                if (neighbourTile == null)
                    stack.Push(neighbour);
            }
        }

        // TODO: If there is nowhere to go, do not place a bomb
        Logger.Warning("I can't find a way to any box tile");
        return [];
    }

    /// <summary>
    /// Finds a path to a tile where the agent can hide from their bomb
    /// </summary>
    private List<GridPosition> FindBombAvoidancePath(
        GridPosition startingPosition,
        BombTile bombTile
    )
    {
        var stack = new Stack<GridPosition>();
        var rnd = new Random();

        // Position is not visited yet if it does not have a parent assigned
        var parents = new GridPosition?[_tileMap.Width, _tileMap.Length];

        // Do not end up on one of the following positions or you will explode!
        // TODO: This does not take into account bomb chain reactions
        // TODO: This does not take into account other bombs and their exploding paths
        var unsafePositions = bombTile
            .ExplosionPaths.Select(explosionPath =>
                explosionPath.TakeWhile(explosionPosition =>
                    _tileMap.GetTile(explosionPosition) == null
                )
            )
            .SelectMany(path => path)
            .Concat([bombTile.Position])
            .ToList();

        stack.Push(startingPosition);
        while (stack.Count != 0)
        {
            var position = stack.Pop();
            var tile = _tileMap.GetTile(position);

            // Maybe we found our safe haven?
            if (tile == null && !unsafePositions.Contains(position))
                return CollectPath(parents, position, startingPosition);

            // We didn't find our safe tile yet, continue searching through tiles
            var neighbours = position
                .Neighbours.Where(neighbour => parents[neighbour.Row, neighbour.Column] == null)
                .ToArray();
            rnd.Shuffle(neighbours);

            foreach (var neighbour in neighbours)
            {
                parents[neighbour.Row, neighbour.Column] = position;
                var neighbourTile = _tileMap.GetTile(neighbour);

                if (neighbourTile == null)
                    stack.Push(neighbour);
            }
        }

        // Accept your fate and don't move
        Logger.Warning("There is no way out of this :(");
        return [];
    }

    /// <param name="from">Where to start collecting the path from (inclusive)</param>
    /// <param name="to">Where to stop collecting the path (inclusive)</param>
    /// <param name="parents">Parent reference array. Each position points to another position where it was discovered from.</param>
    /// <returns>Path in a reversed order</returns>
    private static List<GridPosition> CollectPath(
        GridPosition?[,] parents,
        GridPosition from,
        GridPosition to
    )
    {
        var path = new List<GridPosition> { from };
        var parent = from;

        while (parent != to)
        {
            parent = parents[parent.Row, parent.Column];
            if (parent == null)
                throw new InvalidOperationException(
                    "Encountered a null parent when collecting the path"
                );

            path.Add(parent);
        }

        return path;
    }
}
