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

    private static readonly TimeSpan SimulationStepDeltaTime = TimeSpan.FromSeconds(1.0f / 30);
    private static readonly TimeSpan MaxSimulationTime = TimeSpan.FromSeconds(10);
    private static readonly int MaxSimulationDepth = (int)(
        MaxSimulationTime / SimulationStepDeltaTime
    );

    public Node(GameState initialState)
    {
        _state = new GameState(initialState);
        _parent = null;
        Action = null;
    }

    public Node(GameState initialState, BombermanAction action, TimeSpan actionSimulationTime)
    {
        _parent = null;
        Action = action;
        _state = new GameState(initialState);

        var iterations = (int)(actionSimulationTime / SimulationStepDeltaTime);
        for (int i = 0; i < iterations; i++)
        {
            SimulateSingleAction(
                _state,
                action switch
                {
                    BombermanAction.PlaceBomb => BombermanAction.Stand,
                    _ => action,
                }
            );
        }
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

        if (Children.Count == 0)
            Children.Add(new Node(this, BombermanAction.Stand));

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

        for (int depth = 0; depth < MaxSimulationDepth && !simulationState.Terminated; depth++)
        {
            var possibleActions = simulationState.Agent.GetPossibleActions().ToArray();

            var nextAction =
                possibleActions.Length == 0
                    ? BombermanAction.Stand
                    : possibleActions[rnd.Next(0, possibleActions.Length)];
            SimulateSingleAction(simulationState, nextAction);
        }

        var score = simulationState.Agent.Player.Score;
        score +=
            1000 * (int)Math.Round(simulationState.Agent.Player.Position.X / Constants.TileSize);

        return simulationState.Terminated ? -1000 : score;
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

        return _totalReward / (Visits + 1e-6)
            + 1.41f * MathF.Sqrt(MathF.Log(_parent.Visits) / Visits);
    }

    private static void SimulateSingleAction(GameState simulationState, BombermanAction action)
    {
        simulationState.Agent.ApplyAction(action);
        simulationState.Update(SimulationStepDeltaTime);
    }

    public override string ToString() =>
        $"{{ \"Action\": \"{Action}\", \"Visits\": {Visits}, \"Total reward\": {_totalReward}, \"Children\": [{string.Join(',', Children)}]}}";
}
