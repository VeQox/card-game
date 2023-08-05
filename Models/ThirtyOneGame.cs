using System.Diagnostics.Contracts;
using server.ExtensionMethods;
using server.Utils;

namespace server.Models;

public class ThirtyOneGame
{
    private static readonly List<Card> Deck = CardUtils.GenerateDeck();
    private static readonly Random Rng = new();
    
    private ThirtyOneStateMachine StateMachine { get; }
    private List<Card> Stack { get; set; }
    private List<Card> CommunityCards { get; set; }
    private List<Player> Players { get; }
    private Player? CurrentPlayer { get; set; }
    private Player? CurrentDealer { get; set; }
    private int CurrentRound { get; set; }

    public static async Task<ThirtyOneGame> StartGame(IEnumerable<Client> clients)
    {
        var game = new ThirtyOneGame(clients);
        await game.StateMachine.AdvanceState(Event.Always, game.StartRound);

        return game;
    }
    
    private ThirtyOneGame(IEnumerable<Client> clients)
    {
        StateMachine = new ThirtyOneStateMachine();
        Stack = new List<Card>();
        CommunityCards = new List<Card>();
        Players = clients.Select(client => new Player(client)).ToList();
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
        
        await StateMachine.AdvanceState(Event.Always, NotifyDealer);
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
        var player = Players.Find(player => player.Id == client.Id);
        
        if(player is null || CurrentPlayer is null) return;

        if (player != CurrentPlayer)
        {
            await player.SendAsync(new ErrorMessage("It's not you turn yet"));
            return;
        }

        switch (message.Event)
        {
            case WebSocketClientEvent.DealerAcceptCards:
                await StateMachine.AdvanceState(Event.DealerAcceptCards, DealerAcceptCards);
                break;
            case WebSocketClientEvent.DealerRejectCards:
                await StateMachine.AdvanceState(Event.DealerRejectCards, DealerRejectCards);
                break;
            case WebSocketClientEvent.PlayerSwapCard:
                var swapCardMessage = JsonUtils.Deserialize<PlayerSwapCardMessage>(raw);
                if (swapCardMessage?.PlayerCard is null || 
                    swapCardMessage.CommunityCard is null)
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
                await StateMachine.AdvanceState(Event.PlayerSwapCard,
                    () => Task.FromResult(PlayerSwapCard(swapCardMessage)));
                break;
            case WebSocketClientEvent.PlayerSwapAll:
                var swapAllCardsMessage = JsonUtils.Deserialize<PlayerSwapAllCards>(raw);
                if(swapAllCardsMessage?.Event is null) return;
                
                CurrentPlayer.HasSkipped = false;
                CurrentPlayer.TurnCount++;
                await StateMachine.AdvanceState(Event.PlayerSwapAll,
                    () => Task.FromResult(PlayerSwapAll()));
                break;
            case WebSocketClientEvent.PlayerSkipTurn:
                if (CurrentPlayer.HasSkipped)
                {
                    await player.SendAsync(new ErrorMessage("Player has already skipped"));
                    return;
                }
                
                CurrentPlayer.TurnCount++;
                await StateMachine.AdvanceState(Event.PlayerSkipTurn, PlayerSkipTurn);
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
                await StateMachine.AdvanceState(Event.PlayerLockTurn, PlayerLockTurn);
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private async Task DealerAcceptCards()
    {
        if (CurrentDealer is null) return;

        CommunityCards = Stack.Splice(3);
        
        await Players.Broadcast(new UpdateCommunityCardsMessage(CommunityCards));

        await StateMachine.AdvanceState(Event.Always, SetPlayersCards);
    }
    
    private async Task DealerRejectCards()
    {
        if (CurrentDealer is null) return;

        CommunityCards = CurrentDealer.Hand;
        CurrentDealer.Hand = Stack.Splice(3);

        await CurrentDealer.SendAsync(new UpdatePlayerMessage(CurrentDealer));
        await Players.Broadcast(new UpdateCommunityCardsMessage(CommunityCards));

        await StateMachine.AdvanceState(Event.Always, SetPlayersCards);
    }

    private async Task SetPlayersCards()
    {
        if (CurrentDealer is null) return;
        
        foreach (var player in Players.Where(player => player != CurrentDealer))
        {
            player.Hand = Stack.Splice(3);
            await player.SendAsync(new UpdatePlayerMessage(player));
        }

        await StateMachine.AdvanceState(Event.Always, EvaluateHands);
    }

    private async Task EvaluateHands()
    {
        foreach (var player in Players)
        {
            if (player.Hand.IsFire())
            {
                await StateMachine.AdvanceState(Event.PlayerHasFire, EndRound);
                return;
            }
            if(player.Hand.IsThirtyOne())
            {
                await StateMachine.AdvanceState(Event.PlayerHasThirtyOne, EndRound);
                return;
            }
        }

        await StateMachine.AdvanceState(Event.Always, StartTurn);
    }
    
    private async Task StartTurn()
    {
        CurrentPlayer = NextPlayer();
        
        if(CurrentPlayer.HasLocked)
        {
            await StateMachine.AdvanceState(Event.PlayerHasLocked, EndRound);
            return;
        }

        await StateMachine.AdvanceState(Event.PlayerHasNotLocked, NotifyPlayer);
    }
    
    private async Task NotifyPlayer()
    {
        if (CurrentPlayer is null) return;

        await CurrentPlayer.SendAsync(new NotifyPlayerMessage(CurrentPlayer.Hand, CommunityCards));
    }

    private async Task PlayerSwapAll()
    {
        if(CurrentPlayer is null) return;
        
        (CommunityCards, CurrentPlayer.Hand) = (CurrentPlayer.Hand, CommunityCards);

        await Players.Broadcast(new UpdateCommunityCardsMessage(CommunityCards));
        await CurrentPlayer.SendAsync(new UpdatePlayerMessage(CurrentPlayer));
    }

    private async Task PlayerSwapCard(PlayerSwapCardMessage message)
    {
        if (CurrentPlayer is null) return;
        var (playerCard, communityCard) = message;
        
        if(playerCard is null || communityCard is null) return;

        CurrentPlayer.Hand.Remove(playerCard);
        CurrentPlayer.Hand.Add(communityCard);
        CommunityCards.Remove(communityCard);
        CommunityCards.Add(playerCard);

        await StateMachine.AdvanceState(Event.Always, EvaluateHands);
    }   
    
    private async Task PlayerSkipTurn()
    {
        if (CurrentPlayer is null) return;

        CurrentPlayer.HasSkipped = true;

        await StateMachine.AdvanceState(Event.Always, StartTurn);
    }
    
    private async Task PlayerLockTurn()
    {
        if (CurrentPlayer is null)
        {
            Console.WriteLine("CurrentPlayer is null");
            return;
        }

        CurrentPlayer.HasLocked = true;

        await StateMachine.AdvanceState(Event.Always, StartTurn);
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
        
        if (Players.Count(player => player.Lives != -1) == 1)
        { 
            await StateMachine.AdvanceState(Event.OnePlayerLeft, EndGame);
            return;
        }

        await StateMachine.AdvanceState(Event.MoreThanOnePlayerLeft, StartRound);
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
        
        var minValue = players.Min(player => player.Hand.CalculateValue());
        return players.FindAll(player => Math.Abs(player.Hand.CalculateValue() - minValue) < 0.1);
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