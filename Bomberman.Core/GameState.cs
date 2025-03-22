using Bomberman.Core.Agents;

namespace Bomberman.Core;

public class GameState : IUpdatable
{
    public Agent AgentOne { get; }
    public Agent AgentTwo { get; }

    public TileMap TileMap { get; }

    public bool Terminated => !AgentOne.Player.Alive || !AgentTwo.Player.Alive;

    public GameState(
        Func<GameState, Player, Agent> agentOneFactory,
        Func<GameState, Player, Agent> agentTwoFactory
    )
    {
        var startOne = new GridPosition(Row: 5, Column: 7);
        var startTwo = new GridPosition(Row: 5, Column: 12);
        TileMap = new TileMap(17, 11).WithDefaultTileLayout(startOne, startTwo);
        var playerOne = new Player(startOne, TileMap);
        var playerTwo = new Player(startTwo, TileMap);

        AgentOne = agentOneFactory(this, playerOne);
        AgentTwo = agentTwoFactory(this, playerTwo);
    }

    public GameState(GameState original)
    {
        TileMap = new TileMap(original.TileMap);

        var playerOne = new Player(original.AgentOne.Player, TileMap);
        var playerTwo = new Player(original.AgentTwo.Player, TileMap);

        AgentOne = original.AgentOne.Clone(this, playerOne);
        AgentTwo = original.AgentTwo.Clone(this, playerTwo);
    }

    public void Update(TimeSpan deltaTime)
    {
        if (Terminated)
            return;

        AgentOne.Update(deltaTime);
        AgentTwo.Update(deltaTime);
        TileMap.Update(deltaTime);
    }
}
