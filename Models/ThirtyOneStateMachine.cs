namespace server.Models;

public class ThirtyOneStateMachine : StateMachine<State, Event>
{
    private static readonly Dictionary<State, Dictionary<Event, State>> Transitions = new()
    {
        {
            State.StartGame, new Dictionary<Event, State>()
            {
                { Event.Always, State.StartRound }
            }
        },
        {
            State.StartRound, new Dictionary<Event, State>()
            {
                { Event.Always, State.WaitForDealer }
            }
        },
        {
            State.WaitForDealer, new Dictionary<Event, State>()
            {
                { Event.DealerAcceptCards, State.DealerAcceptedCards },
                { Event.DealerRejectCards, State.DealerRejectedCards }
            }
        },
        {
            State.DealerAcceptedCards, new Dictionary<Event, State>()
            {
                { Event.Always, State.SetPlayersCards }
            }
        },
        {
            State.DealerRejectedCards, new Dictionary<Event, State>
            {
                { Event.Always, State.SetPlayersCards }
            }
        },
        {
            State.SetPlayersCards, new Dictionary<Event, State>()
            {
                { Event.Always, State.EvaluateHands }
            }
        },
        {
            State.EvaluateHands, new Dictionary<Event, State>()
            {
                { Event.PlayerHasThirtyOne, State.EndRound },
                { Event.PlayerHasFire, State.EndRound },
                { Event.Always, State.SetNextPlayer }
            }
        },
        {
            State.SetNextPlayer, new Dictionary<Event, State>()
            {
                { Event.PlayerHasLocked, State.EndRound },
                { Event.PlayerHasNotLocked, State.WaitForPlayer }
            }
        },
        {
            State.WaitForPlayer, new Dictionary<Event, State>()
            {
                { Event.PlayerSwapCard, State.PlayerSwappedCards },
                { Event.PlayerSwapAll, State.PlayerSwappedAll },
                { Event.PlayerSkipTurn, State.PlayerSkippedTurn },
                { Event.PlayerLockTurn, State.PlayerLockedTurn }
            }
        },
        {
            State.PlayerSwappedCards, new Dictionary<Event, State>()
            {
                { Event.Always, State.EvaluateHands }
            }
        },
        {
            State.PlayerSwappedAll, new Dictionary<Event, State>()
            {
                { Event.Always, State.EvaluateHands }
            }
        },
        {
            State.PlayerSkippedTurn, new Dictionary<Event, State>()
            {
                { Event.Always, State.SetNextPlayer }
            }
        },
        {
            State.PlayerLockedTurn, new Dictionary<Event, State>()
            {
                { Event.Always, State.SetNextPlayer }
            }
        },
        {
            State.EndRound, new Dictionary<Event, State>()
            {
                { Event.MoreThanOnePlayerLeft, State.StartRound },
                { Event.OnePlayerLeft, State.EndGame }
            }
        }
    };

    public ThirtyOneStateMachine() : base(State.StartGame, Transitions) { }

    public async Task AdvanceState(Event action, Func<Task> callback)
    { 
        if(base.AdvanceState(action) is false) return;
        await callback();
    }
}

public enum State
{
    StartGame,
    StartRound,
    WaitForDealer,
    DealerAcceptedCards,
    DealerRejectedCards,
    SetPlayersCards,
    EvaluateHands,
    SetNextPlayer,
    WaitForPlayer,
    PlayerSwappedAll,
    PlayerSwappedCards,
    PlayerSkippedTurn,
    PlayerLockedTurn,
    EndRound,
    EndGame
}

public enum Event
{
    Always,
    DealerAcceptCards,
    DealerRejectCards,
    PlayerSwapAll,
    PlayerSwapCard,
    PlayerSkipTurn,
    PlayerLockTurn,
    PlayerHasLocked,
    PlayerHasThirtyOne,
    PlayerHasFire,
    PlayerHasNotLocked,
    MoreThanOnePlayerLeft,
    OnePlayerLeft,
}