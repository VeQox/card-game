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
    
    [HttpGet("{id}")]
    public async Task HandleUpgrade(string id)
    {
        var room = RoomRepository.GetRoom(id);

        if (room is null)
        {
            Logger.LogInformation("Room with {Id} not found", id);
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            Logger.LogInformation("Request not a websocket request");
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        Logger.LogInformation("Connection established");
        var connection = new WebSocketConnection(webSocket);
        var client = new Client(connection, "Guest");
        room.OnConnection(client);
        Logger.LogInformation("Connection established with client[{Guid}]", client.Guid);
        
        try
        {
            while (!webSocket.CloseStatus.HasValue)
            {
                var raw = await connection.ReceiveAsync();
                if (raw is null)
                {
                    Logger.LogInformation("Received null from client[{Guid}]", client.Guid);
                    continue;
                }
                
                var (message, error) = JsonUtils.Deserialize<WebSocketClientMessage>(raw);
                if (message is null || error)
                {
                    Logger.LogInformation("Received invalid message from client[{Guid}]", client.Guid);
                    continue;
                }
                
                Logger.LogInformation("Received {Message} from client[{Guid}]", message, client.Guid);
                await room.OnMessage(client, message, raw);
            }
        }
        finally
        {
            await client.CloseAsync();
            Logger.LogInformation("Connection closed with client[{Guid}]", client.Guid);
        }
    }
}