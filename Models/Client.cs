using Newtonsoft.Json;
using server.Utils;

namespace server.Models;

public class Client : IComparable<Client>
{
    [JsonProperty("id")] public Guid Guid => Connection.Guid;
    [JsonProperty("name")] public string Name { get; set; }
    [JsonIgnore] private WebSocketConnection Connection { get; }
    
    protected Client(Client client) : 
        this(client.Connection, client.Name) {}

    public Client(WebSocketConnection connection, string name)
        => (Connection, Name) = (connection, name);
    
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