using Bomberman.Core.Utilities;

namespace Bomberman.Core.Agents.MCTS;

internal class Node
{
    /// <summary>
    /// The action that was taken from the parent to reach this node
    /// </summary>
    public BombermanAction? Action { get; }
    public List<BombermanAction> UnexploredActions { get; }
    public List<Node> Children { get; } = [];
    public int Visits { get; private set; }
    public double TotalReward { get; private set; }
    public double AverageReward => TotalReward / (Visits + 1e-6);
    public double HeuristicValue { get; }
    public GameState State { get; }

    // DEBUG
    public GameState? SimulationEndState { get; private set; }

    private readonly Node? _parent;
    private readonly Random _rnd = new();
    private readonly MctsAgent _agent;

    public Node(GameState mctsStartingState, int agentIndex, BombermanAction action)
    {
        _parent = null;
        Action = action;
        State = mctsStartingState;
        _agent = (MctsAgent)State.Agents[agentIndex];
        // IMPORTANT: Starting state should be simulated based on previous action determined by MCTS
        _agent.ApplyAction(action);
        AdvanceTimeOneTile(State, _agent);
        UnexploredActions = _agent.GetPossibleActions().ToList();
        HeuristicValue = _agent.CalculateSimulationHeuristic();
    }

    private Node(Node parent, BombermanAction action)
    {
        _parent = parent;
        Action = action;
        State = new GameState(parent.State);
        _agent = (MctsAgent)State.Agents[parent._agent.AgentIndex];
        _agent.ApplyAction(action);
        AdvanceTimeOneTile(State, _agent);
        UnexploredActions = _agent.GetPossibleActions().ToList();
        HeuristicValue = _agent.CalculateSimulationHeuristic();
    }

    public Node Expand()
    {
        if (State.Terminated)
            return this;

        if (UnexploredActions.Count == 0)
            throw new InvalidOperationException(
                "Trying to expand a node that is already fully expanded"
            );

        var actionToExplore = UnexploredActions[_rnd.Next(0, UnexploredActions.Count)];
        UnexploredActions.Remove(actionToExplore);
        var newChild = new Node(this, actionToExplore);
        Children.Add(newChild);

        return newChild;
    }

    public Node Select(double heuristicWeight)
    {
        if (UnexploredActions.Count != 0)
            return this;

        if (State.Terminated)
            return this;

        var bestNode = Children.OrderByDescending(node => node.UCT(heuristicWeight)).First();
        return bestNode.Select(heuristicWeight);
    }

    /// <returns>Reward</returns>
    public double Simulate()
    {
        var simulationState = new GameState(State);
        var simulationAgent = (MctsAgent)simulationState.Agents[_agent.AgentIndex];
        var opponentAgent = simulationState.Agents.First(a => a != simulationAgent);

        const int maxSimulationDepth = 20;

        for (var depth = 0; depth < maxSimulationDepth && !simulationState.Terminated; depth++)
        {
#if DEBUG
            var opponentPosition = opponentAgent.Player.Position.ToGridPosition();
            if (!simulationState.TileMap.IsPositionInsideBounds(opponentPosition))
                throw new InvalidOperationException(
                    "Opponent somehow ended up outside the tile map"
                );
#endif

            var nextAction = simulationAgent.GetSimulationAction();

            simulationAgent.ApplyAction(nextAction);
            AdvanceTimeOneTile(simulationState, simulationAgent);
        }

        SimulationEndState = simulationState;

        if (!simulationAgent.Player.Alive)
            return 0;

        if (!opponentAgent.Player.Alive)
            return 1;

        var distance = simulationState.TileMap.ShortestDistance(
            simulationAgent.Player.Position.ToGridPosition(),
            opponentAgent.Player.Position.ToGridPosition(),
            simulationAgent.Player.Speed
        );

        // distanceScore [0.25; 0.75]
        var distanceScore = ((1.0 - (distance / simulationAgent.MaxDistance)) / 2) + 0.25;

        return distanceScore;
    }

    public void Backpropagate(double reward)
    {
        Visits++;
        TotalReward += reward;

        _parent?.Backpropagate(reward);
    }

    private double UCT(double heuristicWeight)
    {
        if (_parent == null)
            throw new InvalidOperationException(
                "Can't calculate UCB1 on a node that has no parent"
            );

        return AverageReward
            + (1.41f / 1) * MathF.Sqrt(MathF.Log(_parent.Visits) / Visits)
            + heuristicWeight * HeuristicValue;
    }

    /// <summary>
    /// Advance the game with the time it takes for the player to move one tile
    /// </summary>
    private static void AdvanceTimeOneTile(GameState stateToAdvance, MctsAgent agent)
    {
        const double secondsPerFrame = 1.0 / 16;
        var frameDeltaTime = TimeSpan.FromSeconds(secondsPerFrame);

        var oneTileDeltaTimeInSeconds = 1.0 / agent.Player.Speed;

        // Splitting the time it takes to move one tile into constant fps to more closely match the real game
        var frameCount = (int)(oneTileDeltaTimeInSeconds / secondsPerFrame);
        var simulationTimeLeft = oneTileDeltaTimeInSeconds;
        for (int i = 0; i < frameCount; i++)
        {
            stateToAdvance.Update(frameDeltaTime);
            simulationTimeLeft -= secondsPerFrame;
        }

        if (simulationTimeLeft > 1e-10)
            stateToAdvance.Update(TimeSpan.FromSeconds(simulationTimeLeft));
    }
}
