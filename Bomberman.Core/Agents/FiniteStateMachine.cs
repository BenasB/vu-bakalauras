using Bomberman.Core.Utilities;

namespace Bomberman.Core.Agents;

internal class FiniteStateMachine<TState>(
    TState start,
    Func<(TState, TState), bool> allowTransition
)
    where TState : Enum
{
    public TState State { get; private set; } = start;

    public void Transition(TState newState)
    {
        if (!allowTransition((State, newState)))
            throw new InvalidOperationException(
                $"Illegal transition from '{State}' to '{newState}'"
            );

        Logger.Information($"State transition from '{State}' to '{newState}'");

        State = newState;
    }
}
