using System.Diagnostics.Contracts;
using server.Extensions;
using server.Utils;

namespace server.Models;

public class ThirtyOneGame
{
    private static readonly List<Card> Deck = CardUtils.GenerateDeck();
    private static readonly Random Rng = new();
    private ThirtyOneStateMachine StateMachine { get; set; }
    private List<Card> Stack { get; set; }
    private List<Card> CommunityCards { get; set; }
    private List<Player> Players { get; set; }
    private Player? CurrentPlayer { get; set; }
    private Player? CurrentDealer { get; set; }
    private int CurrentRound { get; set; }

    public static async Task<ThirtyOneGame> StartGame(List<Client> clients)
    {
        var game = new ThirtyOneGame(clients);
        if (game.StateMachine.AdvanceState(Event.Always))
        {
            await game.StartRound();
        }

        return game;
    }
    
    private ThirtyOneGame(List<Client> clients)
    {
        StateMachine = new ThirtyOneStateMachine();
        Stack = new List<Card>();
        CommunityCards = new List<Card>();
        Players = clients.FindAll(client => client.HasJoinedRoom).Select(client => new Player(client)).ToList();
        CurrentPlayer = null;
        CurrentDealer = null;
        CurrentRound = 0;
    }
    
    
    private async Task StartRound()
    {
        Stack = Deck.Shuffle(Rng);
        CommunityCards = new List<Card>();
        Players.ForEach(player =>
        {
            player.HasLocked = false;
            player.HasSkipped = false;
            player.TurnCount = 0;
        });
        CurrentDealer = NextDealer();
        CurrentPlayer = CurrentDealer;
        CurrentRound = 0;

        CurrentDealer.Hand = Stack.Splice(3);
        
        if(StateMachine.AdvanceState(Event.Always))
        {
            await NotifyDealer();
        }
    }

    private async Task NotifyDealer()
    {
        if (CurrentDealer is null)
        {
            throw new Exception("CurrentDealer is null");
        }

        await CurrentDealer.SendAsync(new NotifyDealerMessage(CurrentDealer.Hand));
    }

    public async Task OnMessage(Client client, WebSocketClientMessage message,  string raw)
    {
        var player = Players.Find(player => player.Guid == client.Guid);
        
        if(player is null || CurrentPlayer is null) return;

        if (player != CurrentPlayer)
        {
            await player.SendAsync(new ErrorMessage("It's not you turn yet"));
            return;
        }

        switch (message.Event)
        {
            case WebSocketClientEvent.DealerAcceptCards:
                if(StateMachine.AdvanceState(Event.DealerAcceptCards))
                {
                    await DealerAcceptCards();
                }
                break;
            case WebSocketClientEvent.DealerRejectCards:
                if(StateMachine.AdvanceState(Event.DealerRejectCards))
                {
                    await DealerRejectCards();
                }
                break;
            case WebSocketClientEvent.PlayerSwapCard:
                var (swapCardMessage, error) = JsonUtils.Deserialize<PlayerSwapCardMessage>(raw);
                if (swapCardMessage is null || error)
                {
                    await player.SendAsync(new ErrorMessage("Invalid message / format"));
                    return;
                }
                if (!CurrentPlayer.Hand.Contains(swapCardMessage.PlayerCard))
                {
                    await player.SendAsync(new ErrorMessage("You do not have that card"));
                    return;
                }
                if(!CommunityCards.Contains(swapCardMessage.CommunityCard))
                {
                    await player.SendAsync(new ErrorMessage("With who are you trading with?"));
                    return;
                }

                CurrentPlayer.HasSkipped = false;
                CurrentPlayer.TurnCount++;
                if(StateMachine.AdvanceState(Event.PlayerSwapCard))
                {
                    await PlayerSwapCard(swapCardMessage);
                }
                break;
            case WebSocketClientEvent.PlayerSkipTurn:
                if (CurrentPlayer.HasSkipped)
                {
                    await player.SendAsync(new ErrorMessage("Player has already skipped"));
                    return;
                }
                
                CurrentPlayer.TurnCount++;
                if(StateMachine.AdvanceState(Event.PlayerSkipTurn))
                {
                    await PlayerSkipTurn();
                }
                break;
            case WebSocketClientEvent.PlayerLockTurn:
                
                if (CurrentPlayer.TurnCount < 1)
                {
                    await player.SendAsync(new ErrorMessage("You cant lock now"));
                    return;
                }
                if (CurrentPlayer.HasLocked)
                {
                    await player.SendAsync(new ErrorMessage("You have already locked (this round should've ended)"));
                    return;
                }

                CurrentPlayer.HasSkipped = false;
                CurrentPlayer.TurnCount++;
                if(StateMachine.AdvanceState(Event.PlayerLockTurn))
                {
                    await PlayerLockTurn();
                }
                break;
            case WebSocketClientEvent.JoinRoom:
            case WebSocketClientEvent.StartGame:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private async Task DealerAcceptCards()
    {
        if (CurrentDealer is null) return;

        CommunityCards = Stack.Splice(3);
        
        await Players.Broadcast(new UpdateCommunityCardsMessage(CommunityCards));

        if(StateMachine.AdvanceState(Event.Always))
        {
            await SetPlayersCards();
        }
    }
    
    private async Task DealerRejectCards()
    {
        if (CurrentDealer is null) return;

        CommunityCards = CurrentDealer.Hand;
        CurrentDealer.Hand = Stack.Splice(3);

        await CurrentDealer.SendAsync(new UpdatePlayerMessage(CurrentDealer));
        await Players.Broadcast(new UpdateCommunityCardsMessage(CommunityCards));

        if (StateMachine.AdvanceState(Event.Always))
        {
            await SetPlayersCards();
        }
    }

    private async Task SetPlayersCards()
    {
        if (CurrentDealer is null) return;
        
        foreach (var player in Players.Where(player => player != CurrentDealer))
        {
            player.Hand = Stack.Splice(3);
            await player.SendAsync(new UpdatePlayerMessage(player));
        }

        if(StateMachine.AdvanceState(Event.Always))
        {
            await EvaluateHands();
        }
    }

    private async Task EvaluateHands()
    {
        foreach (var player in Players)
        {
            if (player.Hand.IsFire())
            {
                if (StateMachine.AdvanceState(Event.PlayerHasFire))
                {
                    await EndRound();
                }
                return;
            }
            if(player.Hand.IsThirtyOne())
            {
                if (StateMachine.AdvanceState(Event.PlayerHasThirtyOne))
                {
                    await EndRound();
                }
                return;
            }
        }

        if (StateMachine.AdvanceState(Event.Always))
        {
            await StartTurn();
        }
    }
    
    private async Task StartTurn()
    {
        CurrentPlayer = NextPlayer();
        
        if(CurrentPlayer.HasLocked)
        {
            if(StateMachine.AdvanceState(Event.PlayerHasLocked))
            {
                await EndRound();
            }
            return;
        }

        if(StateMachine.AdvanceState(Event.PlayerHasNotLocked))
        {
            await NotifyPlayer();
        }
    }
    
    private async Task NotifyPlayer()
    {
        if (CurrentPlayer is null) return;

        await CurrentPlayer.SendAsync(new NotifyPlayerMessage(CurrentPlayer.Hand, CommunityCards));
    }
    
    private async Task PlayerSwapCard(PlayerSwapCardMessage message)
    {
        if (CurrentPlayer is null)
        {
            Console.WriteLine("CurrentPlayer is null");
            return;
        }

        CurrentPlayer.Hand.Remove(message.PlayerCard);
        CurrentPlayer.Hand.Add(message.CommunityCard);
        CommunityCards.Remove(message.CommunityCard);
        CommunityCards.Add(message.PlayerCard);

        if(StateMachine.AdvanceState(Event.Always))
        {
            await EvaluateHands();
        }
    }
    
    private async Task PlayerSkipTurn()
    {
        if (CurrentPlayer is null) return;

        CurrentPlayer.HasSkipped = true;

        if (StateMachine.AdvanceState(Event.Always))
        {
            await StartTurn();
        }
    }
    
    private async Task PlayerLockTurn()
    {
        if (CurrentPlayer is null)
        {
            Console.WriteLine("CurrentPlayer is null");
            return;
        }

        CurrentPlayer.HasLocked = true;

        if(StateMachine.AdvanceState(Event.Always))
        {   
            await StartTurn();
        }
    }
    
    private async Task EndRound()
    {
        CurrentRound++;

        var losers = EvaluateLosers(Players);
        foreach (var player in losers)
        {
            player.Lives--;
            await player.SendAsync(new UpdatePlayerMessage(player));
        }

        Console.WriteLine(JsonUtils.Serialize(losers));

        if (Players.Count(player => player.Lives != -1) == 1)
        {
            if(StateMachine.AdvanceState(Event.OnePlayerLeft))
            {
                await EndGame();
            }
            return;
        }
        
        if(StateMachine.AdvanceState(Event.MoreThanOnePlayerLeft))
        {
            await StartRound();
        }
    }

    private async Task EndGame()
    {
        var winner = Players.Find(player => player.Lives != -1);
        if (winner is null)
        {
            Console.WriteLine("Winner is null");
            return;
        }
        await Players.Broadcast(new EndGameMessage(winner));
    }

    private static List<Player> EvaluateLosers(List<Player> players)
    {
        if (players.Any(player => player.Hand.IsFire()))
        {
            return players.FindAll(player => !player.Hand.IsFire());
        }
        
        var minValue = players.Min(player => player.Hand.Value());
        return players.FindAll(player => Math.Abs(player.Hand.Value() - minValue) < 0.1);
    }

    private Player NextDealer()
    {
        CurrentDealer = CurrentDealer is null ? 
            Players[Rng.Next(0, Players.Count)] : 
            Players[(Players.IndexOf(CurrentDealer) + 1) % Players.Count];

        return CurrentDealer;
    }
    
    private Player NextPlayer()
    {
        if (CurrentPlayer is not null) return Players[(Players.IndexOf(CurrentPlayer) + 1) % Players.Count];
        throw new Exception("CurrentPlayer is null");
    }
}