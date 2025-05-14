using Bomberman.Core.Agents;

namespace Bomberman.Core;

public class GameState : IUpdatable
{
    public Agent[] Agents { get; }

    public TileMap TileMap { get; }

    public bool Terminated => Agents.Any(a => a is { Player.Alive: false });

    public TimeSpan TimeElapsed { get; private set; } = TimeSpan.Zero;

    public GameState(Func<GameState, Player, int, Agent> agentFactory, Scenario scenario)
    {
        TileMap = scenario.TileMap;
        Agents = new Agent[2];

        for (int i = 0; i < Agents.Length; i++)
        {
            var player = new Player(scenario.StartPositions[i], TileMap);
            Agents[i] = agentFactory(this, player, i);
        }
    }

    public GameState(GameState original)
    {
        TileMap = new TileMap(original.TileMap);
        TimeElapsed = original.TimeElapsed;

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
        TimeElapsed = original.TimeElapsed;

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

        TimeElapsed += deltaTime;

        foreach (var agent in Agents)
            agent.Update(deltaTime);

        TileMap.Update(deltaTime);
    }
}
