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
        var mctsInterval = TimeSpan.FromMilliseconds(200);
        while (!realState.Terminated)
        {
            var root = new Node(realState);
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

            Logger.Information(
                $"Enqueued best action after ({iterations} iterations): {bestAction}"
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

        // TODO: determine where a player can go, e.g. don't lead to exploding bomb

        var gridPosition = Player.Position.ToGridPosition();
        if (_tileMap.GetTile(gridPosition with { Row = gridPosition.Row - 1 }) == null)
            result.Add(BombermanAction.MoveUp);
        if (_tileMap.GetTile(gridPosition with { Row = gridPosition.Row + 1 }) == null)
            result.Add(BombermanAction.MoveDown);
        if (_tileMap.GetTile(gridPosition with { Column = gridPosition.Column - 1 }) == null)
            result.Add(BombermanAction.MoveLeft);
        if (_tileMap.GetTile(gridPosition with { Column = gridPosition.Column + 1 }) == null)
            result.Add(BombermanAction.MoveRight);

        if (Player.CanPlaceBomb && _tileMap.GetTile(gridPosition) == null)
            result.AddRange(
                [
                    BombermanAction.PlaceBombAndMoveUp,
                    BombermanAction.PlaceBombAndMoveDown,
                    BombermanAction.PlaceBombAndMoveLeft,
                    BombermanAction.PlaceBombAndMoveRight,
                ]
            );

        return result;
    }
}
