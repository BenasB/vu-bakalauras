using System.Globalization;
using Bomberman.Core.MCTS;

namespace Bomberman.Core;

public class GameState : IUpdatable
{
    public Agent Agent { get; }

    public TileMap TileMap { get; }

    public bool Terminated => !Agent.Player.Alive;

    private TimeSpan _shiftElapsed = TimeSpan.Zero;
    private int _shiftsSoFar = 0;
    private TimeSpan _shiftInterval;

    public GameState()
    {
        var start = new GridPosition(Row: 5, Column: 7);
        TileMap = new TileMap(17, 11).WithDefaultTileLayout(start);
        Agent = new Agent(start, TileMap, this);
        _shiftInterval = GetShiftInterval(_shiftsSoFar);
    }

    public GameState(GameState original)
    {
        TileMap = new TileMap(original.TileMap);
        Agent = new Agent(original.Agent, TileMap);
        _shiftElapsed = original._shiftElapsed;
        _shiftsSoFar = original._shiftsSoFar;
        _shiftInterval = original._shiftInterval;
    }

    public void Update(TimeSpan deltaTime)
    {
        Agent.Update(deltaTime);
        TileMap.Update(deltaTime);

        _shiftElapsed += deltaTime;

        if (_shiftElapsed < _shiftInterval)
            return;

        _shiftsSoFar++;
        _shiftElapsed = TimeSpan.Zero;
        _shiftInterval = GetShiftInterval(_shiftsSoFar);
        TileMap.Shift();
        Agent.Player.Position = Agent.Player.Position with
        {
            X = Agent.Player.Position.X - 1 * Constants.TileSize,
        };

        // Safety precaution to make sure the player dies
        if (Agent.Player.Position.X < 0)
            Agent.Player.TakeDamage();

        if (Agent.Player.Alive)
            Agent.Player.Score += 10;
    }

    private static TimeSpan GetShiftInterval(int shiftsSoFar) =>
        TimeSpan.FromSeconds(Math.Max(2, 4 * Math.Pow(0.96, shiftsSoFar)));

    // TODO: Expose data about player's parameters (so we know if the player picked up powerups, etc.)
    public override string ToString() =>
        $"{{ \"{nameof(Agent.Player)}\": {{ \"{nameof(Agent.Player.Position)}\": {{ \"{nameof(Agent.Player.Position.X)}\": {Agent.Player.Position.X.ToString(CultureInfo.InvariantCulture)}, \"{nameof(Agent.Player.Position.Y)}\": {Agent.Player.Position.Y.ToString(CultureInfo.InvariantCulture)} }} }}, \"{nameof(TileMap)}\": {TileMap} }}";
}
