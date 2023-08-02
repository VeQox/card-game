using System.Net.WebSockets;
using System.Text;

namespace server.Models;

public class WebSocketConnection
{
    private WebSocket WebSocket { get; }
    
    public WebSocketConnection(WebSocket webSocket)
    {
        WebSocket = webSocket;
    }
    
    public async Task SendAsync(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        var buffer = new ArraySegment<byte>(bytes);
        await WebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    public async Task<string?> ReceiveAsync()
    {
        var buffer = new byte[1024 * 4];
        var payload = new List<byte>(buffer.Length);

        WebSocketReceiveResult? result;

        do
        {
            result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            payload.AddRange(new ArraySegment<byte>(buffer, 0, result.Count));
        } while (!result.EndOfMessage);

        return result.MessageType == WebSocketMessageType.Close ? 
            string.Empty :
            Encoding.UTF8.GetString(payload.ToArray());
    }
    
    public async Task CloseAsync()
    {
        await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
    }
}