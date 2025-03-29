using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Bomberman.Core.Serialization;
using Bomberman.Core.Tiles;
using Bomberman.Core.Utilities;

namespace Bomberman.Core.Agents.MCTS;

public class MctsAgent : Agent
{
    private readonly GameState _state;

    public MctsAgent(GameState state, Player player, int agentIndex)
        : base(player, agentIndex)
    {
        _state = state;
        _ = Task.Run(() => LoopMcts(false));
    }

    private MctsAgent(GameState state, Player player, MctsAgent original)
        : base(player, original.AgentIndex)
    {
        _state = state;
    }

    internal override Agent Clone(GameState state, Player player) =>
        new MctsAgent(state, player, this);

    private void LoopMcts(bool writeJson)
    {
        Logger.Information($"Starting MCTS loop for agent {AgentIndex}");

        var serializationQueue = new BlockingCollection<Node>(new ConcurrentQueue<Node>());

        if (writeJson)
            _ = Task.Run(() => SerializationLoop(serializationQueue));

        var previousAction = BombermanAction.Stand;
        while (!_state.Terminated)
        {
            // IMPORTANT: Starting state should be simulated based on previous action determined by MCTS
            var root = new Node(_state, this, previousAction);

            var iterations = 0;

            // TODO: What about 'Stand' action, should we wait full time?
            // TODO: Make this dependent on real game state instead of time to support non constant fps
            var mctsInterval = TimeSpan.FromSeconds(1 / Player.Speed);
            // TODO: Do we need alignment to coordinates after a single action is performed?

            var stopWatch = Stopwatch.StartNew();
            while (stopWatch.Elapsed < mctsInterval && !_state.Terminated)
            {
                iterations++;
                var selectedNode = root.Select();
                var expandedNode = selectedNode.Expand();
                var reward = expandedNode.Simulate();
                expandedNode.Backpropagate(reward);
            }
            stopWatch.Stop();

            if (_state.Terminated)
                break;

            var bestNode = root.Children.MaxBy(child => child.Visits);
            var bestAction =
                bestNode?.Action
                ?? throw new InvalidOperationException("Could not find the best action");

            // Be aware of concurrency
            ApplyAction(bestAction);
            previousAction = bestAction;

            if (writeJson)
                serializationQueue.Add(root);

            Logger.Information(
                $"Applied best action after ({iterations} iterations): {bestAction}"
            );
        }
    }

    private static void SerializationLoop(BlockingCollection<Node> collection)
    {
        var outputDirectory = $"{DateTimeOffset.Now.Ticks}";
        Directory.CreateDirectory(outputDirectory);

        var jsonOptions = new JsonSerializerOptions { MaxDepth = 1024 };

        while (true)
        {
            var root = collection.Take();
            var dto = root.ToDto();

            File.WriteAllText(
                Path.Combine(outputDirectory, $"{DateTimeOffset.Now.Ticks}.json"),
                JsonSerializer.Serialize(dto, jsonOptions)
            );
        }
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

        var canPlaceBomb = Player.CanPlaceBomb && _state.TileMap.GetTile(gridPosition) is null;

        if (
            _state.TileMap.GetTile(gridPosition with { Row = gridPosition.Row - 1 })
            is null
                or IEnterable
        )
        {
            result.Add(BombermanAction.MoveUp);

            if (canPlaceBomb)
                result.Add(BombermanAction.PlaceBombAndMoveUp);
        }

        if (
            _state.TileMap.GetTile(gridPosition with { Row = gridPosition.Row + 1 })
            is null
                or IEnterable
        )
        {
            result.Add(BombermanAction.MoveDown);

            if (canPlaceBomb)
                result.Add(BombermanAction.PlaceBombAndMoveDown);
        }

        if (
            _state.TileMap.GetTile(gridPosition with { Column = gridPosition.Column - 1 })
            is null
                or IEnterable
        )
        {
            result.Add(BombermanAction.MoveLeft);

            if (canPlaceBomb)
                result.Add(BombermanAction.PlaceBombAndMoveLeft);
        }

        if (
            _state.TileMap.GetTile(gridPosition with { Column = gridPosition.Column + 1 })
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

    internal IEnumerable<BombermanAction> GetPossibleSimulationActions()
    {
        var result = new List<BombermanAction> { BombermanAction.Stand };

        var gridPosition = Player.Position.ToGridPosition();

        var shouldPlaceBomb =
            Player.CanPlaceBomb
            && _state.TileMap.GetTile(gridPosition) is null
            && (
                _state.TileMap.GetTile(gridPosition with { Row = gridPosition.Row - 1 }) is BoxTile
                || _state.TileMap.GetTile(gridPosition with { Row = gridPosition.Row + 1 })
                    is BoxTile
                || _state.TileMap.GetTile(gridPosition with { Column = gridPosition.Column - 1 })
                    is BoxTile
                || _state.TileMap.GetTile(gridPosition with { Column = gridPosition.Column + 1 })
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
            _state.TileMap.GetTile(position) is null or (IEnterable and not ExplosionTile);
    }
}
