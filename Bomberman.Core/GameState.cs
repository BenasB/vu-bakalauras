using Bomberman.Core.Agents;

namespace Bomberman.Core;

public class GameState : IUpdatable
{
    public Agent[] Agents { get; }

    public TileMap TileMap { get; }

    public bool Terminated => Agents.Any(a => !a.Player.Alive);

    public GameState(Func<GameState, Player, int, Agent> agentFactory)
    {
        var startOne = new GridPosition(Row: 1, Column: 1);
        var startTwo = new GridPosition(Row: 5, Column: 12);
        TileMap = new TileMap(17, 11).WithDefaultTileLayout(startOne, startTwo);
        var playerOne = new Player(startOne, TileMap);
        var playerTwo = new Player(startTwo, TileMap);

        var agentOne = agentFactory(this, playerOne, 0);
        var agentTwo = agentFactory(this, playerTwo, 1);

        Agents = [agentOne, agentTwo];
    }

    public GameState(GameState original)
    {
        TileMap = new TileMap(original.TileMap);

        Agents = new Agent[original.Agents.Length];
        for (int i = 0; i < original.Agents.Length; i++)
        {
            var player = new Player(original.Agents[i].Player, TileMap);
            Agents[i] = original.Agents[i].Clone(this, player);
        }
    }

    public void Update(TimeSpan deltaTime)
    {
        if (Terminated)
            return;

        foreach (var agent in Agents)
            agent.Update(deltaTime);

        TileMap.Update(deltaTime);
    }
}
