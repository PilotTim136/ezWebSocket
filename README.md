# Project for easier use of WebSockets

## Overview

This Project allows websocket connection via client and server.

## Features
- **Ping**: The client can send a Ping() command, which the server will reply to, where the client can get the latency between server and client.
- **WebSocket Server**: Handles client connections, receives messages, and sends responses.
-- **WebSocket Client**: Sends messages to the server and calculates the round-trip time for the Ping() method.

## Server Side

The WebSocket server listens for incoming connections and handles messages from clients.
> ***NOTICE***: The example below contains the default values.
### Example Server Code
```csharp
using ezWebSocket.server;

ServerSocket server = new ServerSocket($"ws://{ip}:{port}/"); //creates the server with ServerSocket.
server.dynamicBuffer = true; //default, will create a dynamic buffer on how big the messages can be.
server.dynamicMaxKB = 100; //the max amount of (dynamic)KB the buffer can have (variable).
server.bufferKBSize = 100; //if dynamicBuffer is set to false, the buffer will always be this big.
server.refuseNoSocket = false; //if this is true, the connection via http will display a 400 HTTP error.

server.clientConnect += (client) =>
{
    //when a client connects to the server
};
server.clientDisconnect += (client) =>
{
    //when a client disconnects from the server
};
server.clientMessage += (client, msg) =>
{
    //when a client sends a message to the server
    client.Send("only for this client!"); //send a message, only for the client that sent the message
};
server.Start(); //starts the server
```

## Client Side
The WebSocket client connects to the server, which the client can then get and recieve messages. The client can also create a Ping-request to get the latency.
### Example Client Code
```csharp
using ezWebSocket;

WebSocket client = new WebSocket($"ws://{ip}:{port}/"); //create client with IP and PORT given

client.onMessage += (msg) =>
{
    //when client recieves message from server
};
client.onError += (err) =>
{
    //when an error happens on client code
};
client.onConnect += () =>
{
    //when the client connects to the server
    KeepAlive(client);
};
client.onDisconnect += () =>
{
    //when the client disconnects from the server
};

client.Connect(); //try to connect to the server

void KeepAlive()
{
    while (!client.alive) //while the client is active, keep main thread alive, so the program doesn't exit
    {
        Thread.Sleep(100);
    }
}
```

## How to Use
### Server
1. Create an instance of `ServerSocket`.
2. Call the `Start` method to start listening on a specified URL.
```csharp
ServerSocket server = new ServerSocket($"ws://{ip}:{port}/");
server.Start();
```
### Client
1. Create an instance of `WebSocket`.
2. Use the `Connect` method to connect to the server.
3. Use the `Ping` method to test the latency between the client and server.
```csharp
WebSocket client = new WebSocket($"ws://{ip}:{port}/");

client.onConnect += () =>
{
    Console.WriteLine($"Ping: {client.Ping()}ms");
};

client.Connect();
```

# Explanation
### Client Methods
- `Ping()`: Sends a ping and waits for the response within a timeout.
- `Send();`: Sends a message to the server.
- `Connect();`: Connects to the WebSocket server.
- `Close();`: Closes the WebSocket connection.

### Server Methods
- Listens for Ping-requests.
- Handles backend messages, connections and disconnections.


## License
This project is open source and available under the MIT License.
