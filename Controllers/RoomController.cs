using System.ComponentModel.DataAnnotations;
using System.Net.WebSockets;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using server.Models;
using server.Repositories;
using server.Utils;

namespace server.Controllers;

[Route("api/rooms")]
public class RoomController : ControllerBase
{
    private RoomRepository RoomRepository { get; }
    private ILogger<RoomController> Logger { get; }

    public RoomController(ILogger<RoomController> logger, RoomRepository roomRepository)
        => (Logger, RoomRepository) = (logger, roomRepository);

    [HttpGet]
    public Task<IActionResult> GetRooms()
    {
        var rooms = RoomRepository.GetRooms();
        Logger.LogInformation("GET on /api/rooms returned {Amount} rooms", rooms.Count);
        
        return Task.FromResult<IActionResult>(Ok(rooms));
    }
    
    [HttpGet("{id}")]
    public Task<IActionResult> GetRoom(string id)
    {
        var room = RoomRepository.GetRoom(id);
        Logger.LogInformation("GET on /api/rooms returned {Room}", room?.ToString());
        
        return room is null ? 
            Task.FromResult<IActionResult>(NotFound()) : 
            Task.FromResult<IActionResult>(Ok(JsonUtils.Serialize(room)));
    }
}