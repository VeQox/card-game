using System.Net;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
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
    public async Task HandleConnection(string id)
    {
        var room = RoomRepository.GetRoom(id);
        var username = HttpContext.Request.Query["username"];

        if (!HttpContext.WebSockets.IsWebSocketRequest || 
            username == StringValues.Empty||
            room is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        var connection = new WebSocketConnection(webSocket);
        var client = new Client(connection, username.ToString());

        if (!await room.TryJoinAsync(client))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        await HandleClientMessages(room, client);
    }

    [HttpGet("{id}/reconnect/{clientId}")]
    public async Task HandleReconnect(string id, string clientId)
    {
        var room = RoomRepository.GetRoom(id);
        var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        if (!Guid.TryParse(clientId, out var connectionId) ||
            room is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var connection = new WebSocketConnection(webSocket, connectionId);
        if (room.TryReconnect(connection, out var client) is false ||
            client is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        await client.HandleReconnect();
        await HandleClientMessages(room, client);
    }

    public async Task HandleClientMessages(Room room, Client client)
    {
        try
        {
            do
            {
                var (messageType, raw) = await client.ReceiveAsync(CancellationToken);
                if (messageType == WebSocketMessageType.Close) return;
                
                var message = JsonUtils.Deserialize<WebSocketClientMessage>(raw);
                if (message is null) continue;
                
                await room.OnMessage(client, message, raw);
            } while (client.IsConnected);
        }
        finally
        {
            await client.CloseAsync();
        }
    }
}