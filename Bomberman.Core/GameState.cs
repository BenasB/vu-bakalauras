using Bomberman.Core.Agents;
using Bomberman.Core.Tiles;

namespace Bomberman.Core;

public class GameState : IUpdatable
{
    public Player MctsAgent { get; }
    public RandomAgent RandomAgent { get; }
    public TileMap TileMap { get; }

    private bool Terminated =>
        !MctsAgent.Alive || !RandomAgent.Alive || TileMap.Tiles.All(tile => tile is not BoxTile);

    public GameState()
    {
        TileMap = new TileMap(17, 9, new GridPosition(Row: 3, Column: 3));
        RandomAgent = new RandomAgent(new GridPosition(Row: 3, Column: 3), TileMap);
        MctsAgent = new Player(new GridPosition(Row: 5, Column: 5), TileMap);
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
