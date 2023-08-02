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
    
    public RoomController(RoomRepository roomRepository)
        => RoomRepository = roomRepository;

    [HttpGet]
    public Task<IActionResult> GetRooms()
    {
        return Task.FromResult<IActionResult>(Ok(JsonUtils.Serialize(RoomRepository.GetRooms())));
    }
    
    [HttpGet("{id}")]
    public Task<IActionResult> GetRoom(string id)
    {
        var room = RoomRepository.GetRoom(id);
        return room is null ? 
            Task.FromResult<IActionResult>(NotFound()) : 
            Task.FromResult<IActionResult>(Ok(JsonUtils.Serialize(room)));
    }

    [HttpPost]
    public Task<IActionResult> CreateRoom([FromBody] PostRoomRequestBody body)
    {
        if (!ModelState.IsValid)
        {
            return Task.FromResult<IActionResult>(BadRequest());
        }
        
        var (name, capacity, isPublic) = body;
        
        if (name is null || capacity is null || isPublic is null)
        {
            return Task.FromResult<IActionResult>(BadRequest());
        }
        
        var room = RoomRepository.CreateRoom(name, capacity.Value, isPublic.Value);
        
        Console.WriteLine($"Room[{room.Id}] created");
        
        return Task.FromResult<IActionResult>(Ok(JsonUtils.Serialize(room)));
    }
}

public record struct PostRoomRequestBody(string? Name, int? Capacity, bool? IsPublic);