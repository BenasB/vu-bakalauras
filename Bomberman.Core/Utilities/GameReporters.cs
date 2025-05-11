using System.Text.Json;

namespace Bomberman.Core.Utilities;

public interface IGameReporter
{
    void Report(GameState terminatedState);
}

public class LoggerReporter : IGameReporter
{
    public void Report(GameState state)
    {
        for (int i = 0; i < state.Agents.Length; i++)
        {
            var agent = state.Agents[i];
            Logger.Information(
                $"{agent.GetType().Name} (Agent {i + 1}): Alive = {agent.Player.Alive}, Distance moved = {agent.Player.Statistics.DistanceMoved}, Bombs placed = {agent.Player.Statistics.BombsPlaced}"
            );
        }
    }
}

public class JsonReporter : IGameReporter
{
    private readonly string _filePath;

    public JsonReporter(string filePath)
    {
        _filePath = filePath;
    }

    public void Report(GameState terminatedState)
    {
        var existingReports = File.Exists(_filePath) switch
        {
            true => JsonSerializer.Deserialize<List<Dictionary<string, JsonPlayerReport>>>(
                File.ReadAllText(_filePath)
            ) ?? [],
            false => [],
        };

        var gameReport = terminatedState.Agents.ToDictionary(
            agent => agent.GetType().Name,
            agent => new JsonPlayerReport
            {
                Alive = agent.Player.Alive,
                BombsPlaced = agent.Player.Statistics.BombsPlaced,
                DistanceMoved = agent.Player.Statistics.DistanceMoved,
            }
        );

        existingReports.Add(gameReport);
        var newJsonString = JsonSerializer.Serialize(existingReports);
        File.WriteAllText(_filePath, newJsonString);
    }

    private class JsonPlayerReport
    {
        public required bool Alive { get; init; }
        public required double DistanceMoved { get; init; }
        public required int BombsPlaced { get; init; }
    }
}
