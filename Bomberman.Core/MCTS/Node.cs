using System.Globalization;
using System.Text;
using Bomberman.Core.Utilities;

namespace Bomberman.Core.MCTS;

internal class Node
{
    /// <summary>
    /// The action that was taken from the parent to reach this node
    /// </summary>
    public BombermanAction? Action { get; }
    public List<Node> Children { get; } = [];
    public int Visits { get; private set; }
    public int TotalReward { get; private set; }
    public double AverageReward => TotalReward / (Visits + 1e-6);
    public GameState State { get; }

    private readonly Node? _parent;

    public Node(GameState initialState)
    {
        State = new GameState(initialState);
        _parent = null;
        Action = null;
    }

    public Node(GameState initialState, BombermanAction action)
    {
        State = new GameState(initialState);
        _parent = null;
        Action = action;
        AdvanceTimeOneTile(State);
    }

    private Node(Node parent, BombermanAction action)
    {
        _parent = parent;
        Action = action;
        State = new GameState(parent.State);
        SimulateSingleAction(State, action);
    }

    public Node Expand()
    {
        if (State.Terminated)
            return this;

        if (Children.Count > 0)
            return this; // Node already expanded

        foreach (var action in State.Agent.GetPossibleActions())
        {
            Children.Add(new Node(this, action));
        }

        return Children.First();
    }

    public Node Select()
    {
        if (State.Terminated)
            return this;

        if (Children.Count == 0)
            return this;

        var bestNode = Children.OrderByDescending(node => node.UCT()).First();
        return bestNode.Select();
    }

    /// <returns>Reward</returns>
    public int Simulate()
    {
        var simulationState = new GameState(State);
        var rnd = new Random();

        const int maxSimulationDepth = 40;

        for (int depth = 0; depth < maxSimulationDepth && !simulationState.Terminated; depth++)
        {
            var possibleActions = simulationState.Agent.GetPossibleActions().ToArray();

            // Uniform random moves
            var nextAction = possibleActions[rnd.Next(0, possibleActions.Length)];
            SimulateSingleAction(simulationState, nextAction);
        }

        var score = simulationState.Agent.Player.Score;

        // Reward the player for getting towards the right side
        score += 100 * simulationState.Agent.Player.Position.ToGridPosition().Column;

        if (!simulationState.Terminated)
            score += 1000; // Player is alive, additional points

        return score;
    }

    public void Backpropagate(int reward)
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

        return AverageReward + 1.41f * MathF.Sqrt(MathF.Log(_parent.Visits) / Visits);
    }

    private static void SimulateSingleAction(GameState simulationState, BombermanAction action)
    {
        simulationState.Agent.ApplyAction(action);

        AdvanceTimeOneTile(simulationState);
    }

    /// <summary>
    /// Advance the game with the time it takes for the player to move one tile
    /// </summary>
    private static void AdvanceTimeOneTile(GameState simulationState)
    {
        var deltaTime = TimeSpan.FromSeconds(1 / simulationState.Agent.Player.Speed);
        simulationState.Update(deltaTime);
    }
}
