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

    public void Connect(Client client)
    {
        Clients.Add(client);
    }

    public async Task OnMessage(Client client, WebSocketClientMessage message, string raw)
    {
        switch (message.Event)
        {
            case WebSocketClientEvent.StartGame:
                if(Game is not null) return;
                await Clients.Broadcast(new StartGameResponse());
                
                Game = await ThirtyOneGame.StartGame(Clients);
                break;
            
            default:
                if(Game is null) break;
                await Game.OnMessage(client, message, raw);
                break;
        };
    }
}