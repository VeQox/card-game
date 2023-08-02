namespace server.Models;

public abstract class StateMachine<TState, TEvent> where TEvent : notnull where TState : notnull
{
    private Dictionary<TState, Dictionary<TEvent, TState>> Transitions { get; }
    protected TState State { get; private set; }

    protected StateMachine(TState initialState, Dictionary<TState, Dictionary<TEvent, TState>> transitions)
        => (State, Transitions) = (initialState, transitions);

    public bool AdvanceState(TEvent action)
    {
        if (!Transitions.TryGetValue(State, out var eventMap) ||
            !eventMap.TryGetValue(action, out var newState)) return false;
        State = newState;
        Console.WriteLine($"State changed to {State}");
        return true;
    }
}