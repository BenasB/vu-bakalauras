using Bomberman.Core.Agents;

namespace Bomberman.Core;

public class GameState : IUpdatable
{
    public Agent[] Agents { get; }

    public TileMap TileMap { get; }

    public bool Terminated => Agents.Any(a => a is { Player.Alive: false });

    public GameState(Func<GameState, Player, int, Agent> agentFactory)
    {
        var startOne = new GridPosition(Row: 1, Column: 1);
        var startTwo = new GridPosition(Row: 5, Column: 12);
        TileMap = new TileMap(17, 11).WithDefaultTileLayout(startOne, startTwo);
        var playerOne = new Player(startOne, TileMap);
        var playerTwo = new Player(startTwo, TileMap);

        Agents = new Agent[2];
        Agents[0] = agentFactory(this, playerOne, 0);
        Agents[1] = agentFactory(this, playerTwo, 1);
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

    public GameState(
        GameState original,
        Func<GameState, GameState, Player, int, Agent> agentFactory
    )
    {
        TileMap = new TileMap(original.TileMap);

        Agents = new Agent[original.Agents.Length];
        for (int i = 0; i < original.Agents.Length; i++)
        {
            var player = new Player(original.Agents[i].Player, TileMap);
            Agents[i] = agentFactory(original, this, player, i);
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
