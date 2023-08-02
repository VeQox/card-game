using System.Net;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using server.Models;
using server.Repositories;
using server.Utils;

namespace server.Controllers;

[Route("ws/rooms")]
public class WebSocketRoomController : ControllerBase
{
    private IHostApplicationLifetime Lifetime { get; }
    private RoomRepository RoomRepository { get; }
    private ILogger<WebSocketRoomController> Logger { get; }

    public WebSocketRoomController(IHostApplicationLifetime lifetime, ILogger<WebSocketRoomController> logger, RoomRepository roomRepository)
    {
        /* TODO:
         figure out how to cancel the read loop 
         without throwing an exception
        */
        Lifetime = lifetime;
        Logger = logger;
        RoomRepository = roomRepository;
    }
    
    [HttpGet]
    public async Task HandleUpgrade()
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            Logger.LogInformation("Request not a websocket request");
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var connection = new WebSocketConnection(webSocket);
        Logger.LogInformation("Connection established");
        
        
        Room? room = null;
        Client? client = null;
        try
        {
            while (!webSocket.CloseStatus.HasValue)
            {
                var raw = await connection.ReceiveAsync();
                Logger.LogInformation("Received {Message} from connection[{Guid}]", raw, connection.Guid);
                if(raw is null) continue;

                var clientMessage = JsonUtils.Deserialize<WebSocketClientMessage>(raw);
                if(clientMessage.Error || clientMessage.Value is null) continue;

                if (WebSocketClientEvent.CreateRoom == clientMessage.Value.Event)
                {
                    var createRoomMessage = JsonUtils.Deserialize<CreateRoomMessage>(raw);
                    if(!createRoomMessage.Error || createRoomMessage.Value is null) continue;
                        
                    var (roomName, capacity, isPublic, name) = createRoomMessage.Value;
                    if(roomName is null || capacity is null || isPublic is null || name is null) continue;
                        
                    room = RoomRepository.CreateRoom(roomName, capacity.Value, isPublic.Value, Logger);
                        
                    client = new Client(connection, name);
                    room.Connect(client);

                    await client.SendAsync(new JoinedRoomResponse(room.Id));
                    continue;
                }
                if (WebSocketClientEvent.JoinRoom == clientMessage.Value.Event)
                {
                    var joinRoomMessage = JsonUtils.Deserialize<JoinRoomMessage>(raw);
                    if(joinRoomMessage.Error || joinRoomMessage.Value is null) continue;
                    
                    var (name, roomId) = joinRoomMessage.Value;
                    if(name is null || roomId is null) return;

                    room = RoomRepository.GetRoom(roomId);
                    if(room is null) return; // Send error message (no room found)
                    
                    client = new Client(connection, name);
                    room.Connect(client);
                    
                    await client.SendAsync(new JoinedRoomResponse(room.Id));
                    continue;
                }
                
                if(room is null || client is null) continue;

                await room.OnMessage(client, clientMessage.Value, raw);
            }
        }
        finally
        {
            await connection.CloseAsync();
            Logger.LogInformation("Connection closed with connection[{Guid}]", connection.Guid);
        }
    }
}