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
    public GameState State { get; }

    // DEBUG
    public GameState? SimulationEndState { get; private set; }

    private readonly Node? _parent;
    private readonly Random _rnd = new();
    private readonly MctsAgent _agent;

    public Node(GameState mctsStartingState, int agentIndex, BombermanAction action)
    {
        // TODO: Add the possibility to switch out the opponent logic
        _parent = null;
        Action = action;
        State = mctsStartingState;
        _agent = (MctsAgent)State.Agents[agentIndex];
        AdvanceTimeOneTile(State, _agent);
        UnexploredActions = _agent.GetPossibleActions().ToList();
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

    public Node Select()
    {
        if (UnexploredActions.Count != 0)
            return this;

        if (State.Terminated)
            return this;

        var bestNode = Children.OrderByDescending(node => node.UCT()).First();
        return bestNode.Select();
    }

    /// <returns>Reward</returns>
    public double Simulate()
    {
        var simulationState = new GameState(State);
        var simulationAgent = (MctsAgent)simulationState.Agents[_agent.AgentIndex];
        var opponentAgent = simulationState.Agents.First(a => a != simulationAgent);

        const int maxSimulationDepth = 20;

        var startingDistance = simulationAgent
            .Player.Position.ToGridPosition()
            .ManhattanDistance(opponentAgent.Player.Position.ToGridPosition());

        for (var depth = 0; depth < maxSimulationDepth && !simulationState.Terminated; depth++)
        {
            var nextAction = simulationAgent.GetSimulationAction();

            simulationAgent.ApplyAction(nextAction);
            AdvanceTimeOneTile(simulationState, simulationAgent);
        }

        SimulationEndState = simulationState;

        if (!simulationAgent.Player.Alive)
            return 0;

        if (!opponentAgent.Player.Alive)
            return 1;

        var maxManhattanDistance =
            simulationState.TileMap.Width - 2 + simulationState.TileMap.Height - 2;

        var finishDistance = simulationAgent
            .Player.Position.ToGridPosition()
            .ManhattanDistance(opponentAgent.Player.Position.ToGridPosition());

        // How much distance did we reduce? How closer did we get
        var distanceReduction = startingDistance - finishDistance;

        // distanceScore [0.25; 0.75]
        var distanceScore = (((double)distanceReduction / maxManhattanDistance) / 4) + 0.5;

        return distanceScore;
    }

    public void Backpropagate(double reward)
    {
        Visits++;
        TotalReward += reward;

        _parent?.Backpropagate(reward);
    }

    private double UCT()
    {
        if (_parent == null)
            throw new InvalidOperationException(
                "Can't calculate UCB1 on a node that has no parent"
            );

        return AverageReward + (1.41f / 1) * MathF.Sqrt(MathF.Log(_parent.Visits) / Visits);
    }

    /// <summary>
    /// Advance the game with the time it takes for the player to move one tile
    /// </summary>
    private static void AdvanceTimeOneTile(GameState stateToAdvance, MctsAgent agent)
    {
        const double secondsPerFrame = 1.0 / 10;
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

        if (simulationTimeLeft > 0)
            stateToAdvance.Update(TimeSpan.FromSeconds(simulationTimeLeft));
    }
}
