using System.Diagnostics;
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
        BombermanAction? previousAction = null;
        while (!realState.Terminated)
        {
            // IMPORTANT: Starting state should be simulated based on previous action determined by MCTS
            var root = previousAction is null
                ? new Node(realState)
                : new Node(realState, previousAction.Value);

            var iterations = 0;

            // TODO: What about 'Stand' action, should we wait full time?
            var mctsInterval = TimeSpan.FromSeconds(1 / Player.Speed);
            // TODO: Do we need alignment to coordinates after a single action is performed?

            var stopWatch = Stopwatch.StartNew();
            while (stopWatch.Elapsed < mctsInterval)
            {
                iterations++;
                var selectedNode = root.Select();
                var expandedNode = selectedNode.Expand();
                var reward = expandedNode.Simulate();
                expandedNode.Backpropagate(reward);
            }
            stopWatch.Stop();

            // File.WriteAllText(
            //     $"{DateTimeOffset.Now.Ticks}.json",
            //     root.ToString().Replace("NaN", "\"NaN\"").Replace("Infinity", "\"Infinity\"")
            // );

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
            case BombermanAction.PlaceBombAndMoveUp:
                Player.PlaceBomb();
                Player.SetMovingDirection(Direction.Up);
                break;
            case BombermanAction.PlaceBombAndMoveDown:
                Player.PlaceBomb();
                Player.SetMovingDirection(Direction.Down);
                break;
            case BombermanAction.PlaceBombAndMoveLeft:
                Player.PlaceBomb();
                Player.SetMovingDirection(Direction.Left);
                break;
            case BombermanAction.PlaceBombAndMoveRight:
                Player.PlaceBomb();
                Player.SetMovingDirection(Direction.Right);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    internal IEnumerable<BombermanAction> GetPossibleActions()
    {
        var result = new List<BombermanAction> { BombermanAction.Stand };

        var gridPosition = Player.Position.ToGridPosition();

        var canPlaceBomb = Player.CanPlaceBomb && _tileMap.GetTile(gridPosition) is null;

        if (
            _tileMap.GetTile(gridPosition with { Row = gridPosition.Row - 1 }) is null or IEnterable
        )
        {
            result.Add(BombermanAction.MoveUp);

            if (canPlaceBomb)
                result.Add(BombermanAction.PlaceBombAndMoveUp);
        }

        if (
            _tileMap.GetTile(gridPosition with { Row = gridPosition.Row + 1 }) is null or IEnterable
        )
        {
            result.Add(BombermanAction.MoveDown);

            if (canPlaceBomb)
                result.Add(BombermanAction.PlaceBombAndMoveDown);
        }

        if (
            _tileMap.GetTile(gridPosition with { Column = gridPosition.Column - 1 })
            is null
                or IEnterable
        )
        {
            result.Add(BombermanAction.MoveLeft);

            if (canPlaceBomb)
                result.Add(BombermanAction.PlaceBombAndMoveLeft);
        }

        if (
            _tileMap.GetTile(gridPosition with { Column = gridPosition.Column + 1 })
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
}
