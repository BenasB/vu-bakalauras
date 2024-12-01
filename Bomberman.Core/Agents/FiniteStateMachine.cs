namespace Bomberman.Core.Agents;

internal class FiniteStateMachine<TState>(
    TState start,
    Func<(TState, TState), bool> allowTransition
)
    where TState : Enum
{
    private readonly Func<(TState, TState), bool> _allowTransition = allowTransition;

    internal FiniteStateMachine(FiniteStateMachine<TState> original)
        : this(original.State, original._allowTransition) { }

    public TState State { get; private set; } = start;

    public void Transition(TState newState)
    {
        if (!_allowTransition((State, newState)))
            throw new InvalidOperationException(
                $"Illegal transition from '{State}' to '{newState}'"
            );

        //Logger.Information($"State transition from '{State}' to '{newState}'");

        State = newState;
    }
}
