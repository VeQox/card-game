using Newtonsoft.Json;
using server.Extensions;
using server.Utils;

namespace server.Models;

public class Room
{
    [JsonProperty("id")] public string Id { get; }
    [JsonProperty("name")] public string Name { get; }
    [JsonProperty("capacity")] public int Capacity { get; }
    [JsonProperty("isPublic")] public bool IsPublic { get; }
    [JsonIgnore] private List<Client> Clients { get; }
    [JsonIgnore] public int ConnectedClients => Clients.Count;
    [JsonProperty("createdAt")] public DateTime CreatedAt { get; }
    [JsonIgnore] private ThirtyOneGame? Game { get;  set; }
    
    
    public Room(string id, string name, int capacity, bool isPublic)
    {
        Id = id;
        Name = name;
        Clients = new List<Client>();
        Capacity = capacity;
        IsPublic = isPublic;
        CreatedAt = DateTime.Now;
    }

    public async Task TryJoin(WebSocketConnection connection, JoinRoomMessage? message)
    {
        var name = message?.Name;
        if (name is null)
        {
            await connection.SendAsync(JsonUtils.Serialize(new ErrorMessage("Joining Room failed")));
            return;
        }
        if (HasJoined(connection))
        {
            await connection.SendAsync(JsonUtils.Serialize(new ErrorMessage("Already in room")));
            return;
        }
        
        Clients.Add(new Client(connection, name));
        
        await Clients.Broadcast(new UpdateRoomMessage(Clients));
    }

    public bool HasJoined(WebSocketConnection connection)
    {
        return Clients.Any(client => client.Guid == connection.Guid);
    }

    public async Task OnMessage(WebSocketConnection connection, WebSocketClientMessage message, string raw)
    {
        var client = Clients.Find(client => client.Guid == connection.Guid);
        if(client is null) return;

        if (message.Event == WebSocketClientEvent.StartGame)
        {
            if(Game is not null) return;
            if(Clients.Count < 2) return;
            await Clients.Broadcast(new StartGameResponse());
            Game = await ThirtyOneGame.StartGame(Clients);
            return;
        }
        
        if(Game is null) return;
        await Game.OnMessage(client, message, raw);
        
    }
}