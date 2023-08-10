using Newtonsoft.Json;
using server.Utils;

namespace server.Models;

#region WebSocketClientMessage
public enum WebSocketClientEvent
{
    StartGame,
    AcceptCards,
    RejectCards,
    SwapAll,
    SwapCard,
    SkipTurn,
    LockTurn,
}

public record WebSocketClientMessage(
    [property: JsonProperty("event")] WebSocketClientEvent Event);

public record PlayerSwapAllCards() : WebSocketClientMessage(WebSocketClientEvent.SwapAll);

public record PlayerSwapCardMessage(
    [property: JsonProperty("playerCard")] Card? PlayerCard,
    [property: JsonProperty("communityCard")]
    Card? CommunityCard) :
    WebSocketClientMessage(WebSocketClientEvent.SwapCard);
#endregion

#region WebSocketServerMessage
public enum WebSocketServerEvent
{
    UpdateRoom,
    StartGame,
    NotifyDealer,
    NotifyPlayer,
    UpdatePlayer,
    UpdateHand,
    UpdateCommunityCards,
    EndRound,
    EndGame,
    Error
}

public record WebSocketServerMessage(
    [property: JsonProperty("event")] WebSocketServerEvent Event);

public record UpdateRoomMessage(
    [property: JsonProperty("lobby")] List<Client> Clients) :
    WebSocketServerMessage(WebSocketServerEvent.UpdateRoom);

public record UpdatePlayerMessage(
    [property: JsonProperty("player")] Player Player) :
    WebSocketServerMessage(WebSocketServerEvent.UpdatePlayer);

public record UpdateHandMessage(
    [property: JsonProperty("cards")] List<Card> Hand) :
    WebSocketServerMessage(WebSocketServerEvent.UpdateHand);

public record UpdateCommunityCardsMessage(
    [property: JsonProperty("communityCards")]
    List<Card> CommunityCards) :
    WebSocketServerMessage(WebSocketServerEvent.UpdateCommunityCards);

public record EndRoundMessage(
    [property: JsonProperty("losers")] List<Player> Losers) :
    WebSocketServerMessage(WebSocketServerEvent.EndRound);

public record EndGameMessage(
    [property: JsonProperty("winner")] Client Winner) :
    WebSocketServerMessage(WebSocketServerEvent.EndGame);

public record ErrorMessage(
    [property: JsonProperty("message")] string Message) :
    WebSocketServerMessage(WebSocketServerEvent.Error);
    
#endregion