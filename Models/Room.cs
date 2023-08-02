using Newtonsoft.Json;
using server.Extensions;
using server.Utils;

namespace server.Models;

public class Room
{
    
    [JsonProperty("id")] public string Id { get; }
    [JsonProperty("name")] public string Name { get; }
    [JsonProperty("capacity")] public int Capacity { get; }
    [JsonIgnore] public int ConnectedClients => Clients.Count(client => client.HasJoinedRoom);
    [JsonProperty("isPublic")] public bool IsPublic { get; }
    [JsonIgnore] private List<Client> Clients { get; }
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
    
    public void OnConnection(Client client)
    {
        // Client attempting to connect to room
        Clients.Add(client);
    }

    public async Task OnMessage(Client client, WebSocketClientMessage message, string raw)
    {
        if(!client.HasJoinedRoom) {
            if(message.Event != WebSocketClientEvent.JoinRoom) return;

            var (joinRoomMessage, error) = JsonUtils.Deserialize<JoinRoomMessage>(raw);
            if(joinRoomMessage is null || joinRoomMessage.Name is null || error) return;
            
            client.HasJoinedRoom = true;
            client.Name = joinRoomMessage.Name;
            
            Console.WriteLine($"{client.Guid}: has joined {Id}");
            
            await Clients.Broadcast(new UpdateRoomMessage(Clients));
            return;
        }

        if (message.Event == WebSocketClientEvent.StartGame && Game is null)
        {
            await Clients.Broadcast(new StartGameResponse());
            
            Game = await ThirtyOneGame.StartGame(Clients);
            return;
        }
        
        if(Game is null) return;
        
        await Game.OnMessage(client, message, raw);
    }
    
    public void OnClose(Client client)
    {
        Clients.Remove(client);
        // Attempt reconnect on client side if broken connection
    }
}