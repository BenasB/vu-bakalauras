namespace Bomberman.Core.Utilities;

internal class StatefulRandom
{
    // linear congruential generator (LCG)
    private const int A = 48271;
    private const int M = 2147483647;
    private const int Q = M / A;
    private const int R = M % A;

    private int _state;

    public StatefulRandom()
        : this(Environment.TickCount) { }

    public StatefulRandom(int seed)
    {
        if (seed is <= 0 or int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(seed),
                "Seed must be between 1 and int.MaxValue - 1."
            );
        }
        _state = seed;
    }

    public StatefulRandom(StatefulRandom original)
    {
        _state = original._state;
    }

    public double NextDouble()
    {
        var tempState = A * (_state % Q) - R * (_state / Q);
        if (tempState <= 0)
            tempState += M;
        _state = tempState;

        return (double)_state / M;
    }
}
