using System.Numerics;
using Bomberman.Core.Tiles;
using Bomberman.Core.Utilities;

namespace Bomberman.Core.Agents;

public class RandomAgent(GridPosition startPosition, TileMap tileMap) : IUpdatable
{
    public Vector2 Position => _player.Position;
    public bool Alive => _player.Alive;

    private readonly Player _player = new(startPosition, tileMap);

    public List<GridPosition>? CurrentPath => _walker?.Path;
    private Walker? _walker;
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
        GoingToPlaceBomb,
        MovingAwayFromBomb,
        WaitingForBomb,
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

    private void GoToPlaceBomb()
    {
        _walker ??= new Walker(FindBombPlacementPath(_player.Position.ToGridPosition()), _player);

        if (!_walker.Finished)
        {
            _walker.UpdatePlayerMovingDirection();
            return;
        }
        _walker = null;

        _bombTile = _player.PlaceBomb();
        _stateMachine.Transition(State.MovingAwayFromBomb);
    }

    private void GoToAvoidBomb()
    {
        if (_bombTile == null)
            throw new InvalidOperationException("There is no bomb to avoid");

        _walker ??= new Walker(
            FindBombAvoidancePath(_player.Position.ToGridPosition(), _bombTile),
            _player
        );

        if (!_walker.Finished)
        {
            _walker.UpdatePlayerMovingDirection();
            return;
        }
        _walker = null;

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

    /// <summary>
    /// Finds a path to a tile where the agent should place a bomb
    /// </summary>
    private List<GridPosition> FindBombPlacementPath(GridPosition startingPosition)
    {
        var stack = new Stack<GridPosition>();
        var rnd = new Random();

        // Position is not visited yet if it does not have a parent assigned
        var parents = new GridPosition?[tileMap.Width, tileMap.Length];

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
                var neighbourTile = tileMap.GetTile(neighbour);

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
        var parents = new GridPosition?[tileMap.Width, tileMap.Length];

        // Do not end up on one of the following positions or you will explode!
        // TODO: This does not take into account bomb chain reactions
        // TODO: This does not take into account other bombs and their exploding paths
        var unsafePositions = bombTile
            .ExplosionPaths.Select(explosionPath =>
                explosionPath.TakeWhile(explosionPosition =>
                    tileMap.GetTile(explosionPosition) == null
                )
            )
            .SelectMany(path => path)
            .Concat([bombTile.Position])
            .ToList();

        stack.Push(startingPosition);
        while (stack.Count != 0)
        {
            var position = stack.Pop();
            var tile = tileMap.GetTile(position);

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
                var neighbourTile = tileMap.GetTile(neighbour);

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
