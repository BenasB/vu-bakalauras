using Bomberman.Core.Utilities;

namespace Bomberman.Core.MCTS;

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

    private readonly Node? _parent;
    private readonly Random _rnd = new();

    public Node(GameState initialState)
    {
        State = new GameState(initialState);
        _parent = null;
        Action = null;
        UnexploredActions = Agent.GetPossibleActions(State).ToList();
    }

    public Node(GameState initialState, BombermanAction action)
    {
        State = new GameState(initialState);
        _parent = null;
        Action = action;
        AdvanceTimeOneTile(State);
        UnexploredActions = Agent.GetPossibleActions(State).ToList();
    }

    private Node(Node parent, BombermanAction action)
    {
        _parent = parent;
        Action = action;
        State = new GameState(parent.State);
        SimulateSingleAction(State, action);
        UnexploredActions = Agent.GetPossibleActions(State).ToList();
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
        var startingScore = simulationState.Player.Score;

        const int maxSimulationDepth = 40;

        var depth = 0;
        for (; depth < maxSimulationDepth && !simulationState.Terminated; depth++)
        {
            var possibleActions = Agent.GetPossibleSimulationActions(simulationState).ToArray();

            // Uniform random moves
            var nextAction = possibleActions[_rnd.Next(0, possibleActions.Length)];
            SimulateSingleAction(simulationState, nextAction);
        }

        var scoreGainedDuringSimulation = simulationState.Player.Score - startingScore;

        // Punish for dying early in the simulation
        // Range [0; 1]
        // If the player died, the simulation depth did not reach maxSimulationDepth
        var survivalCoefficient = (double)depth / maxSimulationDepth;

        // Reward the player for getting towards the right side
        var columnReward = 20 * simulationState.Player.Position.ToGridPosition().Column;

        var finalReward = survivalCoefficient * (scoreGainedDuringSimulation + columnReward);

        return finalReward;
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

        return AverageReward + 1.41f * 10 * MathF.Sqrt(MathF.Log(_parent.Visits) / Visits);
    }

    private static void SimulateSingleAction(GameState simulationState, BombermanAction action)
    {
        Agent.ApplyAction(simulationState.Player, action);

        AdvanceTimeOneTile(simulationState);
    }

    /// <summary>
    /// Advance the game with the time it takes for the player to move one tile
    /// </summary>
    private static void AdvanceTimeOneTile(GameState simulationState)
    {
        const double secondsPerFrame = 1.0 / 20;
        var frameDeltaTime = TimeSpan.FromSeconds(secondsPerFrame);

        var oneTileDeltaTimeInSeconds = 1.0 / simulationState.Player.Speed;

        // Splitting the time it takes to move one tile into constant fps to match the real game
        var frameCount = (int)(oneTileDeltaTimeInSeconds / secondsPerFrame);
        for (int i = 0; i < frameCount; i++)
        {
            simulationState.Update(frameDeltaTime);
        }

        var remainingDeltaTime = TimeSpan.FromSeconds(oneTileDeltaTimeInSeconds % secondsPerFrame);

        if (remainingDeltaTime > TimeSpan.Zero)
            simulationState.Update(remainingDeltaTime);
    }
}
