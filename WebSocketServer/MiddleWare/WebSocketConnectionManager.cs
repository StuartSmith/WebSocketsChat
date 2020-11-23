using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace WebSocketServer.MiddleWare
{
    public class WebSocketConnectionManager
    {
        private ConcurrentDictionary<string,WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();

        public ConcurrentDictionary<string,WebSocket> GetAllSockets()        
        {
            return _sockets;
        }

        public string AddSocket(WebSocket socket)
        {
            string ConnID = Guid.NewGuid().ToString();

            _sockets.TryAdd(ConnID,socket);

            Console.WriteLine($"Connection added {ConnID}");
            return ConnID;
        }
    }
}
