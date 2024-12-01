using System.Numerics;
using Bomberman.Core.Utilities;

namespace Bomberman.Core.Agents.Mcts;

internal class Node
{
    public BombermanAction? Action { get; }
    public List<Node> Children { get; } = [];
    public int Visits { get; private set; }

    private readonly GameState _state;
    private readonly Node? _parent;
    private float _totalReward = 0;

    private static readonly TimeSpan TimeStep = TimeSpan.FromSeconds(1.0f / 3);
    private const int IterationsPerTimeStep = 15;
    private static readonly TimeSpan SimulationFrameTime = TimeStep / IterationsPerTimeStep;

    public Node(GameState initialState)
    {
        _state = new GameState(initialState);
        _parent = null;
        Action = null;
    }

    private Node(Node parent, BombermanAction action)
    {
        _parent = parent;
        Action = action;
        _state = new GameState(parent._state);
        SimulateSingleAction(_state, action);
    }

    public Node Expand()
    {
        if (_state.Terminated)
            return this;

        if (Children.Count > 0)
            return this; // Node already expanded

        foreach (var action in _state.MctsAgent.GetPossibleActions())
        {
            Children.Add(new Node(this, action));
        }

        return Children.First();
    }

    public Node Select()
    {
        if (_state.Terminated)
            return this;

        if (Children.Count == 0)
            return this;

        var bestNode = Children.OrderByDescending(node => node.UCT()).First();
        return bestNode.Select();
    }

    /// <returns>Reward</returns>
    public float Simulate()
    {
        var simulationState = new GameState(_state);
        var rnd = new Random();

        var maxDepth = 100;
        for (int depth = 0; depth < maxDepth && !simulationState.Terminated; depth++)
        {
            var possibleActions = simulationState.MctsAgent.GetPossibleActions().ToArray();

            // Next action can be selected using heuristics, not just randomly
            var nextAction = possibleActions[rnd.Next(0, possibleActions.Length)];
            SimulateSingleAction(simulationState, nextAction);
        }

        var result = 0.0f;
        if (simulationState.MctsAgent.Alive)
            result += 100;
        if (!simulationState.MctsAgent.Alive)
            result += -1000;
        if (simulationState.RandomAgent.Alive)
            result += -100;
        if (!simulationState.RandomAgent.Alive)
            result += 200;

        const float pointsPerTile = 20;
        const float maxDistance = 6;
        // Incentivize the mcts agent to be close to the random agent
        var distance =
            Vector2.Distance(
                simulationState.RandomAgent.Position,
                simulationState.MctsAgent.Position
            ) / Constants.TileSize;

        var distanceScore = distance < maxDistance ? (maxDistance - distance) * pointsPerTile : 0;
        result += distanceScore;

        return result;
    }

    public void Backpropagate(float reward)
    {
        Visits++;
        _totalReward += reward;

        _parent?.Backpropagate(reward);
    }

    private double UCT()
    {
        if (_parent == null)
            throw new InvalidOperationException(
                "Can't calculate UCB1 on a node that has no parent"
            );

        return _totalReward / (Visits + 1e-6)
            + 1.41f * MathF.Sqrt(MathF.Log(_parent.Visits) / Visits);
    }

    private static void SimulateSingleAction(GameState simulationState, BombermanAction action)
    {
        simulationState.MctsAgent.ApplyAction(action);

        for (int i = 0; i < IterationsPerTimeStep; i++)
            simulationState.Update(SimulationFrameTime);
    }
}
