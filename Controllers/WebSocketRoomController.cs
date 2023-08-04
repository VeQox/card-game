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
    private CancellationToken CancellationToken { get; }
    private RoomRepository RoomRepository { get; }

    public WebSocketRoomController(IHostApplicationLifetime lifetime, RoomRepository roomRepository)
    {
        CancellationToken = lifetime.ApplicationStopping;
        RoomRepository = roomRepository;
    }
    
    [HttpGet("{id}")]
    public async Task HandleUpgrade(string id)
    {
        var room = RoomRepository.GetRoom(id);
        
        if (!HttpContext.WebSockets.IsWebSocketRequest || room is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var connection = new WebSocketConnection(webSocket);
        
        try
        {
            do
            {
                var (messageType, raw) = await connection.ReceiveAsync(CancellationToken);
                if (messageType == WebSocketMessageType.Close) return;
                
                var (message, error) = JsonUtils.Deserialize<WebSocketClientMessage>(raw);
                if (error || message is null) continue;
                
                if (room.HasJoined(connection))
                {
                    await room.OnMessage(connection, message, raw);
                }
                else if (message.Event == WebSocketClientEvent.JoinRoom)
                {
                    await room.TryJoin(connection, JsonUtils.Deserialize<JoinRoomMessage>(raw).Value);
                }
            } while (!webSocket.CloseStatus.HasValue);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }   
}