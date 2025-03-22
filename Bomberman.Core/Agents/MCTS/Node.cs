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

    private readonly Node? _parent;
    private readonly Random _rnd = new();

    // public Node(GameState initialState, MctsAgent agent, BombermanAction action)
    // {
    //     // TODO: Somehow retrieve MCTS agent?
    //     State = new GameState(initialState);
    //     _parent = null;
    //     Action = action;
    //     AdvanceTimeOneTile(State);
    //     UnexploredActions = _agent.GetPossibleActions().ToList();
    // }
    //
    // private Node(Node parent, BombermanAction action)
    // {
    //     _parent = parent;
    //     Action = action;
    //     State = new GameState(parent.State);
    //     SimulateSingleAction(State, action);
    //     UnexploredActions = MctsAgent.GetPossibleActions(State).ToList();
    // }
    //
    // public Node Expand()
    // {
    //     if (State.Terminated)
    //         return this;
    //
    //     if (UnexploredActions.Count == 0)
    //         throw new InvalidOperationException(
    //             "Trying to expand a node that is already fully expanded"
    //         );
    //
    //     var actionToExplore = UnexploredActions[_rnd.Next(0, UnexploredActions.Count)];
    //     UnexploredActions.Remove(actionToExplore);
    //     var newChild = new Node(this, actionToExplore);
    //     Children.Add(newChild);
    //
    //     return newChild;
    // }
    //
    // public Node Select()
    // {
    //     if (UnexploredActions.Count != 0)
    //         return this;
    //
    //     if (State.Terminated)
    //         return this;
    //
    //     var bestNode = Children.OrderByDescending(node => node.UCT()).First();
    //     return bestNode.Select();
    // }
    //
    // /// <returns>Reward</returns>
    // public double Simulate()
    // {
    //     var simulationState = new GameState(State);
    //
    //     const int maxSimulationDepth = 40;
    //
    //     var depth = 0;
    //     for (; depth < maxSimulationDepth && !simulationState.Terminated; depth++)
    //     {
    //         var possibleActions = MctsAgent.GetPossibleSimulationActions(simulationState).ToArray();
    //
    //         // Uniform random moves
    //         var nextAction = possibleActions[_rnd.Next(0, possibleActions.Length)];
    //         SimulateSingleAction(simulationState, nextAction);
    //     }
    //
    //     return 0.5; // TODO: Score
    // }
    //
    // public void Backpropagate(double reward)
    // {
    //     Visits++;
    //     TotalReward += reward;
    //
    //     _parent?.Backpropagate(reward);
    // }
    //
    // private double UCT()
    // {
    //     if (_parent == null)
    //         throw new InvalidOperationException(
    //             "Can't calculate UCB1 on a node that has no parent"
    //         );
    //
    //     return AverageReward + 1.41f * 10 * MathF.Sqrt(MathF.Log(_parent.Visits) / Visits);
    // }
    //
    // private static void SimulateSingleAction(GameState simulationState, BombermanAction action)
    // {
    //     MctsAgent.ApplyAction(simulationState.Player, action);
    //
    //     AdvanceTimeOneTile(simulationState);
    // }
    //
    // /// <summary>
    // /// Advance the game with the time it takes for the player to move one tile
    // /// </summary>
    // private static void AdvanceTimeOneTile(GameState simulationState)
    // {
    //     const double secondsPerFrame = 1.0 / 20;
    //     var frameDeltaTime = TimeSpan.FromSeconds(secondsPerFrame);
    //
    //     var oneTileDeltaTimeInSeconds = 1.0 / simulationState.Player.Speed;
    //
    //     // Splitting the time it takes to move one tile into constant fps to match the real game
    //     var frameCount = (int)(oneTileDeltaTimeInSeconds / secondsPerFrame);
    //     for (int i = 0; i < frameCount; i++)
    //     {
    //         simulationState.Update(frameDeltaTime);
    //     }
    //
    //     var remainingDeltaTime = TimeSpan.FromSeconds(oneTileDeltaTimeInSeconds % secondsPerFrame);
    //
    //     if (remainingDeltaTime > TimeSpan.Zero)
    //         simulationState.Update(remainingDeltaTime);
    // }
}
