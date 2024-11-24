using Bomberman.Core.Agents;
using Bomberman.Core.Agents.Mcts;
using Bomberman.Core.Tiles;

namespace Bomberman.Core;

public class GameState : IUpdatable
{
    public MctsAgent MctsAgent { get; }
    public RandomAgent RandomAgent { get; }
    public TileMap TileMap { get; }

    private bool Terminated =>
        !MctsAgent.Alive || !RandomAgent.Alive || TileMap.Tiles.All(tile => tile is not BoxTile);

    public GameState()
    {
        TileMap = new TileMap(17, 9, new GridPosition(Row: 3, Column: 3));
        RandomAgent = new RandomAgent(new GridPosition(Row: 3, Column: 3), TileMap);
        MctsAgent = new MctsAgent(new GridPosition(Row: 5, Column: 5), TileMap);
    }

    public GameState(GameState original)
    {
        TileMap = new TileMap(original.TileMap);
        RandomAgent = new RandomAgent(original.RandomAgent, TileMap);
        MctsAgent = new MctsAgent(original.MctsAgent, TileMap);
    }

    public void Update(TimeSpan deltaTime)
    {
        RandomAgent.Update(deltaTime);
        MctsAgent.Update(deltaTime);
        TileMap.Update(deltaTime);
    }

    public Task PlayoutAsync() =>
        Task.Run(() =>
        {
            var deltaTime = TimeSpan.FromSeconds(1.0 / 60);
            while (!Terminated)
            {
                Update(deltaTime);
            }
        });
}
