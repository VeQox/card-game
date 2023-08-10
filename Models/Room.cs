using Newtonsoft.Json;
using server.ExtensionMethods;
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

    public async Task<bool> TryJoinAsync(Client client)
    {
        if (HasJoined(client))
        {
            await client.SendAsync(new ErrorMessage("Already in room"));
            return false;
        }
        
        Clients.Add(client);
        
        await Clients.Broadcast(new UpdateRoomMessage(Clients));
        return true;
    }

    public bool TryReconnect(WebSocketConnection connection, out Client? client)
    {
        client = Clients.Find(client => client.Id == connection.Id);
        if(client is null) return false;

        client.Connection = connection;
        return true;
    }

    private bool HasJoined(Client client)
    {
        return Clients.Any(c => c.Id == client.Id);
    }

    public async Task OnMessage(Client client, WebSocketClientMessage message, string raw)
    {
        if (message.Event is WebSocketClientEvent.StartGame)
        {
            if(Game is not null) return;
            if(Clients.Count < 2) return;
            await Clients.Broadcast(new WebSocketServerMessage(WebSocketServerEvent.StartGame));
            Game = await ThirtyOneGame.StartGame(Clients);
            return;
        }
        
        if(Game is null) return;
        await Game.OnMessage(client, message, raw);
        
    }
}