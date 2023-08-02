using System.Collections;
using System.Reflection;
using Newtonsoft.Json;

namespace server.Models;

public enum WebSocketClientEvent
{
    JoinRoom,
    StartGame,
    DealerAcceptCards,
    DealerRejectCards,
    PlayerSwapCard,
    PlayerSkipTurn,
    PlayerLockTurn,
}

public enum WebSocketServerEvent
{
    UpdateRoom,
    StartGame,
    NotifyDealer,
    NotifyPlayer,
    UpdatePlayer,
    UpdateCommunityCards,
    EndGame,
    Error
}

public record WebSocketClientMessage(
    [property:JsonProperty("event")]WebSocketClientEvent Event);

public record JoinRoomMessage(
    [property:JsonProperty("name")]string? Name) : 
    WebSocketClientMessage(WebSocketClientEvent.JoinRoom);

public record StartGameMessage() : 
    WebSocketClientMessage(WebSocketClientEvent.StartGame);

public record DealerAcceptCardsMessage() : 
    WebSocketClientMessage(WebSocketClientEvent.DealerAcceptCards);

public record DealerRejectCardsMessage() : 
    WebSocketClientMessage(WebSocketClientEvent.DealerRejectCards);

public record PlayerSwapCardMessage(
    [property:JsonProperty("playerCard")]Card PlayerCard, 
    [property:JsonProperty("communityCard")]Card CommunityCard) : 
    WebSocketClientMessage(WebSocketClientEvent.PlayerSwapCard);


public record WebSocketServerMessage(
    [property:JsonProperty("event")]WebSocketServerEvent Event);

public record UpdateRoomMessage(
    [property:JsonProperty("lobby")] List<Client> Clients) : 
    WebSocketServerMessage(WebSocketServerEvent.UpdateRoom);

public record StartGameResponse() :
    WebSocketServerMessage(WebSocketServerEvent.StartGame);

public record StartGameError(
    [property:JsonProperty("error")]string Error) : 
    WebSocketServerMessage(WebSocketServerEvent.StartGame);

public record UpdatePlayerMessage(
    [property:JsonProperty("player")]Client Player) : 
    WebSocketServerMessage(WebSocketServerEvent.UpdatePlayer);

public record UpdateCommunityCardsMessage(
    [property:JsonProperty("communityCards")]List<Card> CommunityCards) : 
    WebSocketServerMessage(WebSocketServerEvent.UpdateCommunityCards);

public record NotifyDealerMessage(
    [property:JsonProperty("hand")]List<Card> Hand) : 
    WebSocketServerMessage(WebSocketServerEvent.NotifyDealer);

public record NotifyPlayerMessage(
    [property:JsonProperty("communityCards")]List<Card> CommunityCards, 
    [property:JsonProperty("hand")]List<Card> Hand) : 
    WebSocketServerMessage(WebSocketServerEvent.NotifyPlayer);
    
public record EndGameMessage(
    [property:JsonProperty("winner")]Client Winner) : 
    WebSocketServerMessage(WebSocketServerEvent.EndGame);

public record ErrorMessage(
    [property:JsonProperty("message")]string Message) : 
    WebSocketServerMessage(WebSocketServerEvent.Error);