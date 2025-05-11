using System.Net.WebSockets;
using System.Text;

namespace ezWebSocket
{
    public class WebSocket
    {
        string url = "";
        private TaskCompletionSource<bool> pingTcs = new TaskCompletionSource<bool>();

        public Action? onConnect, onDisconnect;
        public Action<string>? onMessage, onError;

        ClientWebSocket socket;
        CancellationTokenSource cancellationTokenSource = null!;
        CancellationToken cancellationToken;

        /// <summary>
        /// whether or not to use a dynamic buffer
        /// </summary>
        public bool dynamicBuffer = true;
        /// <summary>
        /// maximum size of the buffer in KB [0 = infinite] (dynamicBuffer has to be true)
        /// </summary>
        public int dynamicMaxKB = 100;
        /// <summary>
        /// size of the buffer in KB (dynamicBuffer has to be false)
        /// </summary>
        public int bufferKBSize = 100;

        public bool alive => socket?.State == WebSocketState.Open;

        /// <summary>
        /// will try to connect via given URL
        /// </summary>
        public WebSocket(string url)
        {
            this.url = url;
            socket = new ClientWebSocket();
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
        }

        /// <summary>
        /// try to connect to the server
        /// </summary>
        public async void Connect()
        {
            try
            {
                await socket.ConnectAsync(new Uri(url), cancellationToken);
                onConnect?.Invoke();

                _ = ReceiveMessages();
            }
            catch (Exception ex)
            {
                onError?.Invoke($"[EZWebSocket] Error connecting: {ex.Message}");
            }
        }

        async Task ReceiveMessages()
        {
            byte[] buffer = GetDynamicBuffer();

            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    if(message != "Pong")
                    {
                        onMessage?.Invoke(message);
                    }
                    else
                    {
                        pingTcs.TrySetResult(true);
                    }
                }
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
            }
            finally
            {
                await CloseAsync();
            }
        }

        private byte[] GetDynamicBuffer()
        {
            int bufferSize = dynamicBuffer ? dynamicMaxKB * 1024 : bufferKBSize * 1024;
            return new byte[bufferSize];
        }

        /// <summary>
        /// Sends a message to the WebSocket server.
        /// </summary>
        public async void Send(string message)
        {
            if (socket.State == WebSocketState.Open)
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cancellationToken);
            }
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        public void Close() => _ = CloseAsync();
        /// <summary>
        /// Closes the connection.
        /// </summary>
        public async Task CloseAsync()
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", cancellationToken);
                onDisconnect?.Invoke();
            }
        }
        public void Stop() => _ = CloseAsync();
        public async Task StopAsync() => await CloseAsync();

        public async Task<long?> PingAsync()
        {
            if (socket.State == WebSocketState.Open)
            {
                pingTcs = new TaskCompletionSource<bool>();

                var start = DateTime.UtcNow;
                Send("ping");

                bool pongReceived = await Task.WhenAny(pingTcs.Task, Task.Delay(5000)) == pingTcs.Task;

                if (pongReceived)
                {
                    return (long)(DateTime.UtcNow - start).TotalMilliseconds;
                }
            }
            return null;
        }

        public long? Ping()
        {
            return PingAsync().GetAwaiter().GetResult();
        }
    }
}
