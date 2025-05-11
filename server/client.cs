using System.Net.WebSockets;
using System.Text;

namespace ezWebSocket.server
{
    public class ServerClient
    {
        public System.Net.WebSockets.WebSocket Socket { get; }
        public CancellationToken Token { get; }

        public ServerClient(System.Net.WebSockets.WebSocket socket, CancellationToken token)
        {
            Socket = socket;
            Token = token;
        }

        public void Send(string message)
        {
            _ = SendAsync(message);
        }

        public async Task SendAsync(string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            await Socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, Token);
        }

        public void Close(string reason = "Closed")
        {
            _ = CloseAsync(reason);
        }

        public async Task CloseAsync(string reason = "Closed")
        {
            await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, Token);
        }
    }
}
