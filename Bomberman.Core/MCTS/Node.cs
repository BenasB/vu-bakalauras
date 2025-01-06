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

    private readonly GameState _state;
    private readonly Node? _parent;
    private int _totalReward = 0;

    private double AverageReward => _totalReward / (Visits + 1e-6);

    public Node(GameState initialState)
    {
        _state = new GameState(initialState);
        _parent = null;
        Action = null;
    }

    public Node(GameState initialState, BombermanAction action)
    {
        _state = new GameState(initialState);
        _parent = null;
        Action = action;
        AdvanceTimeOneTile(_state);
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

        foreach (var action in _state.Agent.GetPossibleActions())
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
    public int Simulate()
    {
        var simulationState = new GameState(_state);
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
        _totalReward += reward;

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

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append('{');

        sb.Append("\"Action\": \"");
        sb.Append(Action);
        sb.Append("\",");

        sb.Append("\"Visits\": ");
        sb.Append(Visits);
        sb.Append(',');

        sb.Append("\"TotalReward\": ");
        sb.Append(_totalReward);
        sb.Append(',');

        sb.Append("\"AverageReward\": ");
        sb.Append(AverageReward.ToString(CultureInfo.InvariantCulture));
        sb.Append(',');

        if (_parent != null)
        {
            sb.Append("\"UCT\": ");
            sb.Append(UCT().ToString(CultureInfo.InvariantCulture));
            sb.Append(',');
        }

        sb.Append("\"State\": ");
        sb.Append(_state);
        sb.Append(',');

        sb.Append("\"Children\": [");
        for (int i = 0; i < Children.Count; i++)
        {
            sb.Append(Children[i]);

            if (i != Children.Count - 1)
                sb.Append(',');
        }
        sb.Append(']');

        sb.Append('}');

        return sb.ToString();
    }
}
