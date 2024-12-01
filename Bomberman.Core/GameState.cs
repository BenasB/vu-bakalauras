using Bomberman.Core.Agents;
using Bomberman.Core.Agents.Mcts;

namespace Bomberman.Core;

public class GameState : IUpdatable
{
    public MctsAgent MctsAgent { get; }
    public RandomAgent RandomAgent { get; }
    public TileMap TileMap { get; }

    public bool Terminated => !MctsAgent.Alive || !RandomAgent.Alive;

    public GameState()
    {
        TileMap = new TileMap(17, 9, new GridPosition(Row: 3, Column: 3));
        RandomAgent = new RandomAgent(new GridPosition(Row: 3, Column: 3), TileMap);
        MctsAgent = new MctsAgent(new GridPosition(Row: 5, Column: 5), TileMap, this);
    }

    public GameState(GameState original)
    {
        TileMap = new TileMap(original.TileMap);
        RandomAgent = new RandomAgent(original.RandomAgent, TileMap);
        MctsAgent = new MctsAgent(original.MctsAgent, TileMap, this);
    }

    public void Update(TimeSpan deltaTime)
    {
        RandomAgent.Update(deltaTime);
        MctsAgent.Update(deltaTime);
        TileMap.Update(deltaTime);
    }
}
