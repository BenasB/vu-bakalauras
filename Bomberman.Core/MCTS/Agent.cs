using System.Diagnostics;
using Bomberman.Core.Tiles;
using Bomberman.Core.Utilities;

namespace Bomberman.Core.MCTS;

public class Agent : IUpdatable
{
    private readonly TileMap _tileMap;
    public Player Player { get; }

    public Agent(GridPosition startPosition, TileMap tileMap, GameState realState)
    {
        _tileMap = tileMap;
        Player = new Player(startPosition, tileMap);

        _ = Task.Run(() => LoopMcts(realState));
    }

    public Agent(Agent original, TileMap tileMap)
    {
        _tileMap = tileMap;
        Player = new Player(original.Player, tileMap);
    }

    private void LoopMcts(GameState realState)
    {
        var mctsInterval = TimeSpan.FromMilliseconds(300);
        BombermanAction? previousAction = null;
        while (!realState.Terminated)
        {
            // IMPORTANT: Starting state should be simulated based on previous action determined by MCTS
            var root =
                previousAction != null
                    ? new Node(realState, previousAction.Value, mctsInterval)
                    : new Node(realState);

            var iterations = 0;

            var stopWatch = Stopwatch.StartNew();
            while (stopWatch.Elapsed < mctsInterval)
            {
                iterations++;
                var selectedNode = root.Select();
                var expandedNode = selectedNode.Expand();
                var reward = expandedNode.Simulate();
                expandedNode.Backpropagate(reward);
            }

            var bestNode = root.Children.MaxBy(child => child.Visits);
            var bestAction =
                bestNode?.Action
                ?? throw new InvalidOperationException("Could not find the best action");

            // Be aware of concurrency
            ApplyAction(bestAction);
            previousAction = bestAction;

            Logger.Information(
                $"Applied best action after ({iterations} iterations): {bestAction}"
            );
        }
    }

    public void Update(TimeSpan deltaTime)
    {
        Player.Update(deltaTime);
    }

    internal void ApplyAction(BombermanAction action)
    {
        switch (action)
        {
            case BombermanAction.MoveUp:
                Player.SetMovingDirection(Direction.Up);
                break;
            case BombermanAction.MoveDown:
                Player.SetMovingDirection(Direction.Down);
                break;
            case BombermanAction.MoveLeft:
                Player.SetMovingDirection(Direction.Left);
                break;
            case BombermanAction.MoveRight:
                Player.SetMovingDirection(Direction.Right);
                break;
            case BombermanAction.Stand:
                Player.SetMovingDirection(Direction.None);
                break;
            case BombermanAction.PlaceBomb:
                Player.PlaceBomb();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    internal IEnumerable<BombermanAction> GetPossibleActions()
    {
        var result = new List<BombermanAction>();

        var gridPosition = Player.Position.ToGridPosition();

        var canStand = true;
        for (int i = 0; i <= Player.BombRange; i++)
        {
            var tile = _tileMap.GetTile(gridPosition with { Row = gridPosition.Row - i });
            if (tile is BombTile)
            {
                canStand = false;
                break;
            }

            if (tile != null)
                break;
        }
        for (int i = 0; i <= Player.BombRange; i++)
        {
            var tile = _tileMap.GetTile(gridPosition with { Row = gridPosition.Row + i });
            if (tile is BombTile)
            {
                canStand = false;
                break;
            }

            if (tile != null)
                break;
        }
        for (int i = 0; i <= Player.BombRange; i++)
        {
            var tile = _tileMap.GetTile(gridPosition with { Column = gridPosition.Column - 1 });
            if (tile is BombTile)
            {
                canStand = false;
                break;
            }

            if (tile != null)
                break;
        }
        for (int i = 0; i <= Player.BombRange; i++)
        {
            var tile = _tileMap.GetTile(gridPosition with { Column = gridPosition.Column + 1 });
            if (tile is BombTile)
            {
                canStand = false;
                break;
            }

            if (tile != null)
                break;
        }

        if (canStand)
            result.Add(BombermanAction.Stand);

        var canMoveUp =
            _tileMap.GetTile(gridPosition with { Row = gridPosition.Row - 1 })
                is null
                    or BombUpTile
                    or CoinTile
                    or FireUpTile
                    or SpeedUpTile;
        var inBombRadiusUp = false;
        for (int i = 1; i <= Player.BombRange + 1; i++)
        {
            var tile = _tileMap.GetTile(gridPosition with { Row = gridPosition.Row - i });
            if (tile is BombTile)
            {
                inBombRadiusUp = true;
                break;
            }

            // Assuming that any tile will break the explosion
            if (tile != null)
                break;
        }

        var canMoveDown =
            _tileMap.GetTile(gridPosition with { Row = gridPosition.Row + 1 })
                is null
                    or BombUpTile
                    or CoinTile
                    or FireUpTile
                    or SpeedUpTile;
        var inBombRadiusDown = false;
        for (int i = 1; i <= Player.BombRange + 1; i++)
        {
            var tile = _tileMap.GetTile(gridPosition with { Row = gridPosition.Row + i });
            if (tile is BombTile)
            {
                inBombRadiusDown = true;
                break;
            }

            if (tile != null)
                break;
        }

        var canMoveLeft =
            _tileMap.GetTile(gridPosition with { Column = gridPosition.Column - 1 })
                is null
                    or BombUpTile
                    or CoinTile
                    or FireUpTile
                    or SpeedUpTile;
        var inBombRadiusLeft = false;
        for (int i = 1; i <= Player.BombRange + 1; i++)
        {
            var tile = _tileMap.GetTile(gridPosition with { Column = gridPosition.Column - i });
            if (tile is BombTile)
            {
                inBombRadiusLeft = true;
                break;
            }

            if (tile != null)
                break;
        }

        var canMoveRight =
            _tileMap.GetTile(gridPosition with { Column = gridPosition.Column + 1 })
                is null
                    or BombUpTile
                    or CoinTile
                    or FireUpTile
                    or SpeedUpTile;
        var inBombRadiusRight = false;
        for (int i = 1; i <= Player.BombRange + 1; i++)
        {
            var tile = _tileMap.GetTile(gridPosition with { Column = gridPosition.Column + i });
            if (tile is BombTile)
            {
                inBombRadiusRight = true;
                break;
            }

            if (tile != null)
                break;
        }

        if (canMoveUp && !inBombRadiusUp)
            result.Add(BombermanAction.MoveUp);
        if (canMoveDown && !inBombRadiusDown)
            result.Add(BombermanAction.MoveDown);
        if (canMoveLeft && !inBombRadiusLeft)
            result.Add(BombermanAction.MoveLeft);
        if (canMoveRight && !inBombRadiusRight)
            result.Add(BombermanAction.MoveRight);

        if (Player.CanPlaceBomb && _tileMap.GetTile(gridPosition) == null)
        {
            var willExplodeSomething = false;
            for (int i = 1; i <= Player.BombRange; i++)
            {
                var tile = _tileMap.GetTile(gridPosition with { Row = gridPosition.Row - i });
                if (tile is BoxTile)
                {
                    willExplodeSomething = true;
                    break;
                }
            }
            for (int i = 0; i <= Player.BombRange; i++)
            {
                var tile = _tileMap.GetTile(gridPosition with { Row = gridPosition.Row + i });
                if (tile is BoxTile)
                {
                    willExplodeSomething = true;
                    break;
                }
            }
            for (int i = 0; i <= Player.BombRange; i++)
            {
                var tile = _tileMap.GetTile(gridPosition with { Column = gridPosition.Column - 1 });
                if (tile is BoxTile)
                {
                    willExplodeSomething = true;
                    break;
                }
            }
            for (int i = 0; i <= Player.BombRange; i++)
            {
                var tile = _tileMap.GetTile(gridPosition with { Column = gridPosition.Column + 1 });
                if (tile is BoxTile)
                {
                    willExplodeSomething = true;
                    break;
                }
            }

            if (willExplodeSomething)
            {
                result.Add(BombermanAction.PlaceBomb);
            }
        }

        return result;
    }
}
