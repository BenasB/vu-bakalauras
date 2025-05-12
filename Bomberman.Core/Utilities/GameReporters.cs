using System.Text.Json;
using System.Text.Json.Serialization;
using Bomberman.Core.Agents;
using Bomberman.Core.Agents.MCTS;

namespace Bomberman.Core.Utilities;

public interface IGameReporter
{
    void Report(GameState state);
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

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public JsonReporter(string filePath)
    {
        _filePath = filePath;
    }

    public void Report(GameState state)
    {
        var existingReports = File.Exists(_filePath) switch
        {
            true => JsonSerializer.Deserialize<List<Dictionary<string, object>>>(
                File.ReadAllText(_filePath)
            ) ?? [],
            false => [],
        };

        const double mctsIterationLowerPecentile = 0.75;

        var gameReport = state.Agents.ToDictionary<Agent?, string, object>(
            agent => agent.GetType().Name,
            agent => new JsonPlayerReport
            {
                Alive = agent.Player.Alive,
                BombsPlaced = agent.Player.Statistics.BombsPlaced,
                DistanceMoved = agent.Player.Statistics.DistanceMoved,
                AverageIterations = agent switch
                {
                    MctsAgent mctsAgent => mctsAgent
                        .MctsRunner.IterationCounts.OrderBy(c => c)
                        .Take(
                            (int)
                                Math.Floor(
                                    mctsAgent.MctsRunner.IterationCounts.Count
                                        * mctsIterationLowerPecentile
                                )
                        )
                        .Average(),
                    _ => null,
                },
            }
        );

        gameReport.Add(nameof(GameState.Terminated), state.Terminated);

        existingReports.Add(gameReport);
        var newJsonString = JsonSerializer.Serialize(existingReports, JsonOptions);
        File.WriteAllText(_filePath, newJsonString);
    }

    private class JsonPlayerReport
    {
        public required bool Alive { get; init; }
        public required double DistanceMoved { get; init; }
        public required int BombsPlaced { get; init; }
        public double? AverageIterations { get; init; }
    }
}
