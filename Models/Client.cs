using Newtonsoft.Json;
using server.Utils;

namespace server.Models;

public class Client : IComparable<Client>
{
    [JsonProperty("id")] public Guid Guid { get; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonIgnore] private WebSocketConnection Connection { get; }
    [JsonIgnore] public bool HasJoinedRoom { get; set; }
    
    
    public Client(WebSocketConnection connection, string name) : 
        this(Guid.NewGuid(), connection, false, name) {}

    protected Client(Client client) : 
        this(client.Guid, client.Connection, client.HasJoinedRoom, client.Name) {}

    protected Client(Guid guid, WebSocketConnection connection, bool hasJoinedRoom, string name)
        => (Guid, Connection, HasJoinedRoom, Name) = (guid, connection, hasJoinedRoom, name);
    
    public async Task SendAsync(WebSocketServerMessage message)
    {
        await Connection.SendAsync(JsonUtils.Serialize(message));
    }

    public async Task CloseAsync()
    {
        await Connection.CloseAsync();
    }

    public static bool operator ==(Client left, Client right)
        => left.Equals(right);
    
    public static bool operator !=(Client left, Client right)
        => !left.Equals(right);

    private bool Equals(Client other)
        => Guid.Equals(other.Guid);

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Client)obj);
    }

    public override int GetHashCode()
        => HashCode.Combine(Guid);

    public int CompareTo(Client? other)
        => Guid.CompareTo(other?.Guid);
}