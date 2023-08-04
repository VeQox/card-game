using System.Net.WebSockets;
using System.Text;

namespace server.Models;

public class WebSocketConnection
{
    private readonly byte[] _buffer = new byte[1024 * 4];
    private readonly List<byte> _payload = new(1024 * 4);
    
    private WebSocket WebSocket { get; }
    public Guid Guid { get; }

    public WebSocketConnection(WebSocket webSocket)
        => (WebSocket, Guid) = (webSocket, Guid.NewGuid());
    
    public async Task SendAsync(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        var buffer = new ArraySegment<byte>(bytes);
        await WebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task<(WebSocketMessageType, string)> ReceiveAsync(CancellationToken cancellationToken)
    {
        _payload.Clear();

        try
        {
            WebSocketReceiveResult? result;

            do
            {
                result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(_buffer), cancellationToken);
                _payload.AddRange(new ArraySegment<byte>(_buffer, 0, result.Count));
            } while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await CloseAsync();
            }
            
            return (result.MessageType, Encoding.UTF8.GetString(_payload.ToArray()));
        }
        catch (OperationCanceledException)
        {
            return (WebSocketMessageType.Close, string.Empty);
        }
    }
    
    public async Task CloseAsync()
    {
        if(WebSocket.State is WebSocketState.Aborted or WebSocketState.Closed) return;
        await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
    }
}