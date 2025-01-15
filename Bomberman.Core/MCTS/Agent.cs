using System.Diagnostics;
using Bomberman.Core.Tiles;
using Bomberman.Core.Utilities;

namespace Bomberman.Core.MCTS;

public static class Agent
{
    public static void LoopMcts(GameState realState)
    {
        BombermanAction? previousAction = null;
        while (!realState.Terminated)
        {
            // IMPORTANT: Starting state should be simulated based on previous action determined by MCTS
            var root = previousAction is null
                ? new Node(realState)
                : new Node(realState, previousAction.Value);

            var iterations = 0;

            // TODO: What about 'Stand' action, should we wait full time?
            // TODO: Make this dependent on real game state instead of time to support non constant fps
            var mctsInterval = TimeSpan.FromSeconds(1 / realState.Player.Speed);
            // TODO: Do we need alignment to coordinates after a single action is performed?

            var stopWatch = Stopwatch.StartNew();
            while (stopWatch.Elapsed < mctsInterval && !realState.Terminated)
            {
                iterations++;
                var selectedNode = root.Select();
                var expandedNode = selectedNode.Expand();
                var reward = expandedNode.Simulate();
                expandedNode.Backpropagate(reward);
            }
            stopWatch.Stop();

            if (realState.Terminated)
                break;

            // File.WriteAllText(
            //     $"{DateTimeOffset.Now.Ticks}.json",
            //     JsonSerializer.Serialize(root.ToDto())
            // );

            var bestNode = root.Children.MaxBy(child => child.Visits);
            var bestAction =
                bestNode?.Action
                ?? throw new InvalidOperationException("Could not find the best action");

            // Be aware of concurrency
            ApplyAction(realState.Player, bestAction);
            previousAction = bestAction;

            Logger.Information(
                $"Applied best action after ({iterations} iterations): {bestAction}"
            );
        }
    }

    internal static void ApplyAction(Player player, BombermanAction action)
    {
        switch (action)
        {
            case BombermanAction.MoveUp:
                player.SetMovingDirection(Direction.Up);
                break;
            case BombermanAction.MoveDown:
                player.SetMovingDirection(Direction.Down);
                break;
            case BombermanAction.MoveLeft:
                player.SetMovingDirection(Direction.Left);
                break;
            case BombermanAction.MoveRight:
                player.SetMovingDirection(Direction.Right);
                break;
            case BombermanAction.Stand:
                player.SetMovingDirection(Direction.None);
                break;
            case BombermanAction.PlaceBombAndMoveUp:
                player.PlaceBomb();
                player.SetMovingDirection(Direction.Up);
                break;
            case BombermanAction.PlaceBombAndMoveDown:
                player.PlaceBomb();
                player.SetMovingDirection(Direction.Down);
                break;
            case BombermanAction.PlaceBombAndMoveLeft:
                player.PlaceBomb();
                player.SetMovingDirection(Direction.Left);
                break;
            case BombermanAction.PlaceBombAndMoveRight:
                player.PlaceBomb();
                player.SetMovingDirection(Direction.Right);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    internal static IEnumerable<BombermanAction> GetPossibleActions(GameState state)
    {
        var result = new List<BombermanAction> { BombermanAction.Stand };

        var gridPosition = state.Player.Position.ToGridPosition();

        var canPlaceBomb = state.Player.CanPlaceBomb && state.TileMap.GetTile(gridPosition) is null;

        if (
            state.TileMap.GetTile(gridPosition with { Row = gridPosition.Row - 1 })
            is null
                or IEnterable
        )
        {
            result.Add(BombermanAction.MoveUp);

            if (canPlaceBomb)
                result.Add(BombermanAction.PlaceBombAndMoveUp);
        }

        if (
            state.TileMap.GetTile(gridPosition with { Row = gridPosition.Row + 1 })
            is null
                or IEnterable
        )
        {
            result.Add(BombermanAction.MoveDown);

            if (canPlaceBomb)
                result.Add(BombermanAction.PlaceBombAndMoveDown);
        }

        if (
            state.TileMap.GetTile(gridPosition with { Column = gridPosition.Column - 1 })
            is null
                or IEnterable
        )
        {
            result.Add(BombermanAction.MoveLeft);

            if (canPlaceBomb)
                result.Add(BombermanAction.PlaceBombAndMoveLeft);
        }

        if (
            state.TileMap.GetTile(gridPosition with { Column = gridPosition.Column + 1 })
            is null
                or IEnterable
        )
        {
            result.Add(BombermanAction.MoveRight);

            if (canPlaceBomb)
                result.Add(BombermanAction.PlaceBombAndMoveRight);
        }

        return result;
    }

    internal static IEnumerable<BombermanAction> GetPossibleSimulationActions(GameState state)
    {
        var result = new List<BombermanAction> { BombermanAction.Stand };

        var gridPosition = state.Player.Position.ToGridPosition();

        var shouldPlaceBomb =
            state.Player.CanPlaceBomb
            && state.TileMap.GetTile(gridPosition) is null
            && (
                state.TileMap.GetTile(gridPosition with { Row = gridPosition.Row - 1 }) is BoxTile
                || state.TileMap.GetTile(gridPosition with { Row = gridPosition.Row + 1 })
                    is BoxTile
                || state.TileMap.GetTile(gridPosition with { Column = gridPosition.Column - 1 })
                    is BoxTile
                || state.TileMap.GetTile(gridPosition with { Column = gridPosition.Column + 1 })
                    is BoxTile
            );

        if (IsTileSafeToWalk(gridPosition with { Row = gridPosition.Row - 1 }))
        {
            result.Add(BombermanAction.MoveUp);

            if (shouldPlaceBomb)
                result.Add(BombermanAction.PlaceBombAndMoveUp);
        }

        if (IsTileSafeToWalk(gridPosition with { Row = gridPosition.Row + 1 }))
        {
            result.Add(BombermanAction.MoveDown);

            if (shouldPlaceBomb)
                result.Add(BombermanAction.PlaceBombAndMoveDown);
        }

        if (IsTileSafeToWalk(gridPosition with { Column = gridPosition.Column - 1 }))
        {
            result.Add(BombermanAction.MoveLeft);

            if (shouldPlaceBomb)
                result.Add(BombermanAction.PlaceBombAndMoveLeft);
        }

        if (IsTileSafeToWalk(gridPosition with { Column = gridPosition.Column + 1 }))
        {
            result.Add(BombermanAction.MoveRight);

            if (shouldPlaceBomb)
                result.Add(BombermanAction.PlaceBombAndMoveRight);
        }

        return result;

        bool IsTileSafeToWalk(GridPosition position) =>
            state.TileMap.GetTile(position) is (null or IEnterable) and not LavaTile;
    }
}
