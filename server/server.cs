using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace ezWebSocket.server
{
    public class ServerSocket
    {
        string url = "";
        /// <summary>
        /// whether or not the listener is listening
        /// </summary>
        public bool alive => listener.IsListening;
        /// <summary>
        /// whether or not to refuse connections without a socket (400)
        /// </summary>
        public bool refuseNoSocket = false;

        public Action<ServerClient>? clientConnect, clientDisconnect;
        public Action<ServerClient, string>? clientMessage;

        //buffer data
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

        HttpListener listener = null!;
        CancellationTokenSource cancellationTokenSource = null!;
        CancellationToken cancellationToken;
        List<ServerClient> clients = new List<ServerClient>();

        /// <summary>
        /// creates webSocket instance with given URL
        /// </summary>
        /// <param name="url">string-url to start at</param>
        public ServerSocket(string url)
        {
            this.url = url;
            Initialize();
        }

        void Initialize()
        {
            listener = new HttpListener();
            listener.Prefixes.Add(url.Replace("ws", "http").Replace("wss", "https"));
        }

        /// <summary>
        /// start the websocket server
        /// </summary>
        public void Start()
        {
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;

            listener.Start();
            _ = ListenLoop();
        }

        async Task ListenLoop()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context = await listener.GetContextAsync();

                if (context.Request.IsWebSocketRequest)
                {
                    _ = ProcessWebSocket(context);
                }
                else if (refuseNoSocket)
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }

        async Task ProcessWebSocket(HttpListenerContext context)
        {
            WebSocketContext wsContext = await context.AcceptWebSocketAsync(null);
            System.Net.WebSockets.WebSocket socket = wsContext.WebSocket;
            ServerClient client = new ServerClient(socket, cancellationToken);

            clients.Add(client);
            clientConnect?.Invoke(client);

            int bufferSize = GetDynamicBufferSize();
            byte[] buffer = new byte[bufferSize];

            while (socket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                
                if(result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", cancellationToken);
                    clients.Remove(client);
                    clientDisconnect?.Invoke(client);
                }
                else
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    if(message != "ping")
                    {
                        clientMessage?.Invoke(client, message);
                    }
                    else
                    {
                        client.Send("Pong");
                    }
                }
            }
        }
        int GetDynamicBufferSize()
        {
            if (dynamicBuffer)
            {
                return dynamicMaxKB == 0 ? int.MaxValue : dynamicMaxKB * 1024;
            }
            else
            {
                return bufferKBSize * 1024;
            }
        }

        /// <summary>
        /// send a message to all clients
        /// </summary>
        public void SendAll(string message)
        {
            _ = SendAllAsync(message);
        }
        /// <summary>
        /// send a message to all clients
        /// </summary>
        public async Task SendAllAsync(string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);

            foreach (var client in clients)
            {
                if (client.Socket.State == WebSocketState.Open)
                {
                    await client.Socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cancellationToken);
                }
            }
        }

        /// <summary>
        /// stop the server
        /// </summary>
        public void Stop()
        {
            cancellationTokenSource?.Cancel();

            try
            {
                listener?.Stop();
                listener?.Close();
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EZWebSocket] Error while stopping: {ex.Message}");
            }
        }
    }
}
