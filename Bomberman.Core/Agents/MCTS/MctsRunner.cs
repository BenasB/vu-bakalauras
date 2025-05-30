﻿using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using System.Threading.Channels;
using Bomberman.Core.Serialization;
using Bomberman.Core.Utilities;

namespace Bomberman.Core.Agents.MCTS;

public class MctsRunner : IUpdatable
{
    private static readonly string SerializationOutputDirectory = $"{DateTimeOffset.Now.Ticks}";

    private readonly GameState _state;
    private readonly MctsAgent _mctsAgent;
    private readonly MctsAgentOptions _options;

    private bool _firstStateWriteDone;
    private GridPosition? _target;
    private Vector2 _lastPosition;
    private bool _ignoreNextAction = false;

    internal readonly List<int> IterationCounts = [];

    private readonly Channel<BombermanAction> _actionChannel =
        Channel.CreateBounded<BombermanAction>(
            new BoundedChannelOptions(1) { SingleReader = true, SingleWriter = true }
        );
    private readonly Channel<(GameState, BombermanAction?)> _stateChannel = Channel.CreateBounded<(
        GameState,
        BombermanAction?
    )>(new BoundedChannelOptions(1) { SingleReader = true, SingleWriter = true });

    public MctsRunner(GameState state, MctsAgent mctsAgent, MctsAgentOptions options)
    {
        _state = state;
        _mctsAgent = mctsAgent;
        _options = options;

        _ = Task.Run(async () =>
        {
            try
            {
                await LoopMcts();
            }
            catch (Exception e)
            {
                Logger.Warning(string.Join(Environment.NewLine, e.Message, e.StackTrace));
            }
        });
    }

    public void Update(TimeSpan deltaTime)
    {
        if (!_firstStateWriteDone)
        {
            _firstStateWriteDone = true;
            if (!_stateChannel.Writer.TryWrite((new GameState(_state, CreateAgent), null)))
                throw new InvalidOperationException(
                    "Unable to kick off the MCTS process by passing the first state"
                );

            _ = WaitAndWriteStateAsync();
        }

        if (_target != null)
        {
            if (_target.NearPosition(_mctsAgent.Player.Position, 0.1))
            {
                _target = null;
                if (!_stateChannel.Writer.TryWrite((new GameState(_state, CreateAgent), null)))
                    throw new InvalidOperationException("Unable to pass the game state to MCTS");
                _mctsAgent.ApplyAction(BombermanAction.Stand);
            }

            if (_lastPosition == _mctsAgent.Player.Position)
            {
                Logger.Warning(
                    "Agent did not move although they had a target, restart the MCTS process"
                );
                _target = null;
                const BombermanAction waitingAction = BombermanAction.Stand;
                if (
                    !_stateChannel.Writer.TryWrite(
                        (new GameState(_state, CreateAgent), waitingAction)
                    )
                )
                    throw new InvalidOperationException(
                        "Unable to pass the game state to MCTS to restart the MCTS process"
                    );
                _mctsAgent.ApplyAction(waitingAction);
                // Ignore the next action because it is based on wrong assumptions (starting position)
                _ignoreNextAction = true;
                _ = WaitAndWriteStateAsync();
            }
        }

        _lastPosition = _mctsAgent.Player.Position;

        if (!_actionChannel.Reader.TryRead(out var action))
            return;

        if (_ignoreNextAction)
        {
            Logger.Warning("Ignoring action");
            _ignoreNextAction = false;
            return;
        }

        _mctsAgent.ApplyAction(action);
        var currentPlayerPosition = _mctsAgent.Player.Position.ToGridPosition();
        _target = MctsAgent.GetGridPositionAfterAction(currentPlayerPosition, action);

        if (_target != currentPlayerPosition)
            return;

        _target = null;
        _ = WaitAndWriteStateAsync();
    }

    private async Task WaitAndWriteStateAsync()
    {
        var waitTime = 1 / _mctsAgent.Player.Speed;
        await Task.Delay(TimeSpan.FromSeconds(waitTime));
        if (!_stateChannel.Writer.TryWrite((new GameState(_state, CreateAgent), null)))
        {
            Logger.Warning("Unable to pass the game state to MCTS after waiting");
            throw new InvalidOperationException(
                "Unable to pass the game state to MCTS after waiting"
            );
        }
        _mctsAgent.ApplyAction(BombermanAction.Stand);
    }

    private Agent CreateAgent(
        GameState originalState,
        GameState newState,
        Player player,
        int agentIndex
    )
    {
        if (agentIndex == _mctsAgent.AgentIndex)
            return originalState.Agents[agentIndex].Clone(newState, player);

        return (_options.OpponentType, originalState.Agents[agentIndex]) switch
        {
            (null, StaticAgent) => originalState.Agents[agentIndex].Clone(newState, player),
            (null, WalkingAgent) => originalState.Agents[agentIndex].Clone(newState, player),
            (null, BombingAgent) => originalState.Agents[agentIndex].Clone(newState, player),
            (null, BombingAgent2) => originalState.Agents[agentIndex].Clone(newState, player),
            (null, _) => throw new NotSupportedException(
                "This opponent type is not supported in MCTS, you must replace it"
            ),
            (AgentType.Static, _) => new StaticAgent(player, agentIndex),
            (AgentType.Walking, _) => new WalkingAgent(newState, player, agentIndex),
            (AgentType.Bombing, _) => new BombingAgent(newState, player, agentIndex),
            (AgentType.Bombing2, _) => new BombingAgent2(newState, player, agentIndex),
            (_, _) => throw new NotSupportedException(
                "This agent type is not supported for replacement in MCTS"
            ),
        };
    }

    private async Task LoopMcts()
    {
        Logger.Information($"Starting MCTS loop for agent {_mctsAgent.AgentIndex}");

        var serializationChannel = Channel.CreateUnbounded<Node>();

        if (_options.Export)
            _ = Task.Run(() => SerializationLoop(serializationChannel));

        await _stateChannel.Reader.WaitToReadAsync();

        var previousAction = BombermanAction.Stand;
        while (!_state.Terminated)
        {
            if (!_stateChannel.Reader.TryRead(out var item))
                throw new InvalidOperationException(
                    "Something went wrong with the data exchange between MCTS loop and the agent"
                );

            var (mctsStartingState, previousActionOverwrite) = item;

            if (previousActionOverwrite != null)
                previousAction = previousActionOverwrite.Value;

            var root = new Node(mctsStartingState, _mctsAgent.AgentIndex, previousAction);

            var iterations = 0;
            var rootAgent = root.State.Agents[_mctsAgent.AgentIndex];
            var rootOpponent = root.State.Agents.First(agent => agent != rootAgent);
            var distance = root.State.TileMap.ShortestDistance(
                rootAgent.Player.Position.ToGridPosition(),
                rootOpponent.Player.Position.ToGridPosition(),
                rootAgent.Player.Speed
            ); // The closer the root player is, the less effect it should have during this MCTS run
            var selectionHeuristicWeight = _options.SelectionHeuristicWeightCoefficient * distance;
            Logger.Information(
                $"Selection heuristic weight for this run: {selectionHeuristicWeight}"
            );
            var stopwatch = new Stopwatch();
            while (!_stateChannel.Reader.TryPeek(out _) && !_state.Terminated)
            {
                iterations++;
                var selectedNode = root.Select(heuristicWeight: selectionHeuristicWeight);
                var expandedNode = selectedNode.Expand();
                var reward = expandedNode.Simulate();
                expandedNode.Backpropagate(reward);

                if (_options.SlowDownTicks != null)
                {
                    stopwatch.Restart();
                    while (stopwatch.ElapsedTicks < _options.SlowDownTicks) { }
                    stopwatch.Stop();
                }
            }

            IterationCounts.Add(iterations);

            if (_state.Terminated)
                break;

            var bestNode = root.Children.MaxBy(child => child.Visits);
            var bestAction = bestNode?.Action;

            if (bestAction == null)
            {
                bestAction = BombermanAction.Stand;
                Logger.Information("Could not find the best action, choosing to stand");
            }

            if (!_actionChannel.Writer.TryWrite(bestAction.Value))
                throw new InvalidOperationException(
                    "Something went wrong with the data exchange between MCTS loop and the agent"
                );

            previousAction = bestAction.Value;

            if (_options.Export)
                serializationChannel.Writer.TryWrite(root);

            Logger.Information($"Found best action after ({iterations} iterations): {bestAction}");
        }
    }

    private async Task SerializationLoop(ChannelReader<Node> reader)
    {
        Directory.CreateDirectory(SerializationOutputDirectory);

        var jsonOptions = new JsonSerializerOptions { MaxDepth = 1024 };

        await foreach (var root in reader.ReadAllAsync())
        {
            var dto = root.ToDto();

            await File.WriteAllTextAsync(
                Path.Combine(
                    SerializationOutputDirectory,
                    $"{_mctsAgent.AgentIndex}-{DateTimeOffset.Now.Ticks}.json"
                ),
                JsonSerializer.Serialize(dto, jsonOptions)
            );
        }
    }
}
