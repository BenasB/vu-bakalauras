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
    private readonly MctsAgentOptions _options;
    private static readonly string SerializationOutputDirectory = $"{DateTimeOffset.Now.Ticks}";

    private readonly Random _rnd = new();
    private readonly int _maxDistance;

    public MctsAgent(GameState state, Player player, int agentIndex, MctsAgentOptions options)
        : base(player, agentIndex)
    {
        _state = state;
        _options = options;
        _maxDistance = new GridPosition(0, 0).ManhattanDistance(
            new GridPosition(state.TileMap.Height / 2, state.TileMap.Width / 2)
        );
        _ = Task.Run(LoopMcts);
    }

    private MctsAgent(GameState state, Player player, MctsAgent original)
        : base(player, original.AgentIndex)
    {
        _options = original._options;
        _maxDistance = original._maxDistance;
        _state = state;
    }

    internal override Agent Clone(GameState state, Player player) =>
        new MctsAgent(state, player, this);

    private void LoopMcts()
    {
        Logger.Information($"Starting MCTS loop for agent {AgentIndex}");

        var serializationQueue = new BlockingCollection<Node>(new ConcurrentQueue<Node>());

        if (_options.Export)
            _ = Task.Run(() => SerializationLoop(serializationQueue));

        var previousAction = BombermanAction.Stand;
        while (!_state.Terminated)
        {
            // IMPORTANT: Starting state should be simulated based on previous action determined by MCTS
            var mctsStartingState = new GameState(_state, CreateAgent);
            var root = new Node(mctsStartingState, AgentIndex, previousAction);

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

            if (_options.Export)
                serializationQueue.Add(root);

            Logger.Information(
                $"Applied best action after ({iterations} iterations): {bestAction}"
            );
        }
    }

    private void SerializationLoop(BlockingCollection<Node> collection)
    {
        Directory.CreateDirectory(SerializationOutputDirectory);

        var jsonOptions = new JsonSerializerOptions { MaxDepth = 1024 };

        while (!collection.IsAddingCompleted)
        {
            var root = collection.Take();
            var dto = root.ToDto();

            File.WriteAllText(
                Path.Combine(
                    SerializationOutputDirectory,
                    $"{AgentIndex}-{DateTimeOffset.Now.Ticks}.json"
                ),
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

    internal BombermanAction GetSimulationAction(GridPosition previousPosition)
    {
        var possibilities = new List<BombermanAction> { BombermanAction.Stand };

        var gridPosition = Player.Position.ToGridPosition();

        var opponent = _state.Agents.First(a => a != this).Player;
        var opponentGridPosition = opponent.Position.ToGridPosition();

        // TODO: Take into account the bomb's range
        var shouldPlaceBomb =
            _state.TileMap.GetTile(gridPosition) is null
            && (
                gridPosition with { Row = gridPosition.Row - 1 } == opponentGridPosition
                || gridPosition with { Row = gridPosition.Row + 1 } == opponentGridPosition
                || gridPosition with { Column = gridPosition.Column - 1 } == opponentGridPosition
                || gridPosition with { Column = gridPosition.Column + 1 } == opponentGridPosition
                || _state.TileMap.GetTile(gridPosition with { Row = gridPosition.Row - 1 })
                    is BoxTile
                || _state.TileMap.GetTile(gridPosition with { Row = gridPosition.Row + 1 })
                    is BoxTile
                || _state.TileMap.GetTile(gridPosition with { Column = gridPosition.Column - 1 })
                    is BoxTile
                || _state.TileMap.GetTile(gridPosition with { Column = gridPosition.Column + 1 })
                    is BoxTile
            );

        var actionsPerDirection = new[]
        {
            (BombermanAction.MoveUp, BombermanAction.PlaceBombAndMoveUp),
            (BombermanAction.MoveDown, BombermanAction.PlaceBombAndMoveDown),
            (BombermanAction.MoveLeft, BombermanAction.PlaceBombAndMoveLeft),
            (BombermanAction.MoveRight, BombermanAction.PlaceBombAndMoveRight),
        };
        foreach (var (action, equivalentMovementAction) in actionsPerDirection)
        {
            if (!IsTileSafeToWalk(GetGridPositionAfterAction(gridPosition, action)))
                continue;

            possibilities.Add(action);

            if (Player.CanPlaceBomb && shouldPlaceBomb)
                possibilities.Add(equivalentMovementAction);
        }

        return possibilities[_rnd.Next(0, possibilities.Count)];

        bool IsTileSafeToWalk(GridPosition position) =>
            _state.TileMap.GetTile(position) is null or (IEnterable and not ExplosionTile);
    }

    internal double SimulationHeuristic(GridPosition playerPosition, GridPosition opponentPosition)
    {
        var distance = opponentPosition.ManhattanDistance(playerPosition);

        return 1 - (double)distance / _maxDistance;
    }

    /// <summary>
    /// Does not take into account physics, just calculates the grid position
    /// </summary>
    private static GridPosition GetGridPositionAfterAction(
        GridPosition position,
        BombermanAction action
    ) =>
        action switch
        {
            BombermanAction.MoveUp or BombermanAction.PlaceBombAndMoveUp => position with
            {
                Row = position.Row - 1,
            },
            BombermanAction.MoveDown or BombermanAction.PlaceBombAndMoveDown => position with
            {
                Row = position.Row + 1,
            },
            BombermanAction.MoveLeft or BombermanAction.PlaceBombAndMoveLeft => position with
            {
                Column = position.Column - 1,
            },
            BombermanAction.MoveRight or BombermanAction.PlaceBombAndMoveRight => position with
            {
                Column = position.Column + 1,
            },
            BombermanAction.Stand => position with { },
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
        };

    private Agent CreateAgent(
        GameState originalState,
        GameState newState,
        Player player,
        int agentIndex
    )
    {
        if (agentIndex == AgentIndex)
            return originalState.Agents[agentIndex].Clone(newState, player);

        return (_options.OpponentType, originalState.Agents[agentIndex]) switch
        {
            (null, StaticAgent) => originalState.Agents[agentIndex].Clone(newState, player),
            (null, WalkingAgent) => originalState.Agents[agentIndex].Clone(newState, player),
            (null, _) => throw new NotSupportedException(
                "This opponent type is not supported in MCTS, you must replace it"
            ),
            (AgentType.Static, _) => new StaticAgent(player, agentIndex),
            (AgentType.Walking, _) => new WalkingAgent(newState, player, agentIndex),
            (_, _) => throw new NotSupportedException(
                "This agent type is not supported for replacement in MCTS"
            ),
        };
    }
}
