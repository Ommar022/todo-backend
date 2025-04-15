using System;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TODO.Sockets
{
    public class WebSocketHandler
    {
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();
        private readonly ConcurrentBag<WebSocket> _sockets2 = new();

        public void AddSocket(WebSocket socket)
        {
            _sockets2.Add(socket);
            Console.WriteLine($"Socket added to _sockets2. Total update sockets: {_sockets2.Count}");
        }

        public async Task HandleWebSocket(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }

            var token = context.Request.Query["token"];
            if (string.IsNullOrEmpty(token))
            {
                context.Response.StatusCode = 401;
                Console.WriteLine("WebSocket connection rejected: No token provided.");
                return;
            }

            string userId = GetUserIdFromToken(token);
            if (string.IsNullOrEmpty(userId))
            {
                context.Response.StatusCode = 401;
                Console.WriteLine("WebSocket connection rejected: Invalid or expired token.");
                return;
            }

            WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
            _sockets.TryAdd(userId, socket);
            _sockets2.Add(socket);
            Console.WriteLine($"WebSocket connected for user: {userId} | Total connections: {_sockets.Count} | Update sockets: {_sockets2.Count}");

            await ReceiveMessages(socket, async (message) =>
            {
                Console.WriteLine($"Received message from user {userId}: {message}");
            });

            _sockets.TryRemove(userId, out _);
            Console.WriteLine($"WebSocket disconnected for user: {userId} | Remaining connections: {_sockets.Count} | Update sockets: {_sockets2.Count}");
        }

        public async Task<bool> BroadcastMessage(string message)
        {
            if (_sockets.IsEmpty)
            {
                Console.WriteLine("No WebSocket connections to broadcast to.");
                return false;
            }

            byte[] buffer = Encoding.UTF8.GetBytes(message);
            var tasks = new List<Task<bool>>();

            foreach (var socket in _sockets.ToArray())
            {
                if (socket.Value.State == WebSocketState.Open)
                {
                    tasks.Add(SendMessageAsync(socket.Value, buffer, socket.Key));
                }
                else
                {
                    Console.WriteLine($"Skipping closed WebSocket connection for user {socket.Key}.");
                    _sockets.TryRemove(socket.Key, out _);
                }
            }

            try
            {
                var results = await Task.WhenAll(tasks);
                Console.WriteLine($"Broadcasted message to {results.Count(r => r)} clients: {message}");
                return results.Any(r => r);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error broadcasting message: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendMessageToUserAsync(string userId, string message)
        {
            if (!_sockets.TryGetValue(userId, out WebSocket socket))
            {
                Console.WriteLine($"No WebSocket connection found for user {userId}");
                return false;
            }

            if (socket.State != WebSocketState.Open)
            {
                Console.WriteLine($"WebSocket for user {userId} is not open.");
                _sockets.TryRemove(userId, out _);
                return false;
            }

            byte[] buffer = Encoding.UTF8.GetBytes(message);
            return await SendMessageAsync(socket, buffer, userId);
        }

        public async Task SendUpdateAsync(string message)
        {
            if (_sockets2.IsEmpty)
            {
                Console.WriteLine("No WebSocket connections available to send update.");
                return;
            }

            var buffer = Encoding.UTF8.GetBytes(message);
            var openSockets = _sockets2.Where(s => s.State == WebSocketState.Open).ToList();
            Console.WriteLine($"Sending update to {openSockets.Count} clients: {message}");

            foreach (var socket in openSockets)
            {
                try
                {
                    await socket.SendAsync(
                        new ArraySegment<byte>(buffer),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending update to socket: {ex.Message}");
                }
            }
        }

        private async Task<bool> SendMessageAsync(WebSocket socket, byte[] buffer, string userId)
        {
            try
            {
                await socket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
                Console.WriteLine($"Sent message to user {userId}: {Encoding.UTF8.GetString(buffer)}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message to user {userId}: {ex.Message}");
                _sockets.TryRemove(userId, out _);
                return false;
            }
        }

        private async Task ReceiveMessages(WebSocket socket, Func<string, Task> handleMessage)
        {
            byte[] buffer = new byte[1024 * 4];
            while (socket.State == WebSocketState.Open)
            {
                try
                {
                    WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await handleMessage(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving message: {ex.Message}");
                    break;
                }
            }
        }

        private string GetUserIdFromToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                return userId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decoding token: {ex.Message}");
                return null;
            }
        }
    }
}