using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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
    public IActionResult GetRooms()
    {
        var rooms = RoomRepository.GetRooms();
        Logger.LogInformation("GET on /api/rooms returned {Amount} rooms", rooms.Count);

        return Ok(rooms);
    }
    
    [HttpGet("{id}")]
    public IActionResult GetRoom(string id)
    {
        var room = RoomRepository.GetRoom(id);
        Logger.LogInformation("GET on /api/rooms returned {Room}", room?.ToString());
        
        return room is null ? NotFound() : Ok(room);
    }

    [HttpPost]
    public IActionResult CreateRoom([FromBody] PostRoomRequestBody body)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        var (name, capacity, isPublic) = body;

        if (name is null || capacity is null || isPublic is null)
        {
            return BadRequest();
        }

        var room = RoomRepository.CreateRoom(name, capacity.Value, isPublic.Value);

        return Ok(room.Id);
    }
}

public record PostRoomRequestBody(
    [property: JsonProperty("name")] string? Name,
    [property: JsonProperty("capacity")] int? Capacity,
    [property: JsonProperty("isPublic")] bool? IsPublic);