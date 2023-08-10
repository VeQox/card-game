using System.Net.WebSockets;
using Newtonsoft.Json;
using server.Utils;

namespace server.Models;

public class Client : IComparable<Client>
{
    [JsonProperty("id")] public Guid Id => Connection.Id;
    [JsonProperty("name")] public string Name { get; set; }
    [JsonIgnore] public WebSocketConnection Connection { get; set;  }
    [JsonIgnore] private Queue<WebSocketServerMessage> OfflineMessageBuffer { get; }
    [JsonIgnore] public bool IsConnected => Connection.IsConnectionAlive;
    
    protected Client(Client client) : 
        this(client.Connection, client.Name) {}

    public Client(WebSocketConnection connection, string name)
        => (Connection, Name, OfflineMessageBuffer) = (connection, name, new Queue<WebSocketServerMessage>());
    
    public async Task SendAsync(WebSocketServerMessage message)
    {
        if (!await Connection.SendAsync(JsonUtils.Serialize(message)))
        {
            OfflineMessageBuffer.Enqueue(message);
        }
    }

    public async Task<(WebSocketMessageType, string)> ReceiveAsync(CancellationToken cancellationToken)
    {
        return await Connection.ReceiveAsync(cancellationToken);
    }
    
    public async Task<bool> CloseAsync()
    {
        return await Connection.CloseAsync();
    }

    public async Task HandleReconnect()
    {
        while (OfflineMessageBuffer.TryDequeue(out var message))
        {
            await SendAsync(message);
        }
    }

    public static bool operator ==(Client left, Client right)
        => left.Equals(right);
    
    public static bool operator !=(Client left, Client right)
        => !left.Equals(right);

    private bool Equals(Client other)
        => Id.Equals(other.Id);

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Client)obj);
    }

    public override int GetHashCode()
        => HashCode.Combine(Id);

    public int CompareTo(Client? other)
        => Id.CompareTo(other?.Id);
}