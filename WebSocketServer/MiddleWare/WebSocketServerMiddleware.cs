using System;
using System.Linq;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Text.Json;

using System.Collections.Generic;

namespace WebSocketServer.MiddleWare
{
    public class WebSocketMsg{
        public string From { get; set; }
        public string To { get; set; }
        public string Message { get; set; }
    }
    public class WebSocketServerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly WebSocketConnectionManager _manager;
        public WebSocketServerMiddleware(RequestDelegate  next, 
        WebSocketConnectionManager manager)
        {
            _next = next;
            _manager= manager;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            WriteRequestParam(context); 

            if (context.WebSockets.IsWebSocketRequest)
                {
                    WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    Console.WriteLine("WebSocket Connected");

                    string ConnID = _manager.AddSocket(webSocket);
                    await SendConnIDAsync(webSocket,ConnID);

                    await ReceiveMessage(webSocket,async(result, buffer)=>{
                            
                            //await Task.Delay(1); 
                            
                            if(result.MessageType == WebSocketMessageType.Text)
                            {
                                Console.WriteLine("Message Recieved");
                                string EncodedMsg = Encoding.UTF8.GetString(buffer,0, result.Count);
                                Console.WriteLine($"Message: {EncodedMsg}");

                                await RouteJSONMessageAsync(EncodedMsg);
                            }
                            else if(result.MessageType == WebSocketMessageType.Close)
                            {
                                var id = _manager.GetAllSockets().FirstOrDefault(s => s.Value == webSocket).Key;
                                
                                _manager.GetAllSockets().TryRemove(id,out WebSocket sock);
                                
                                  await sock.CloseOutputAsync(result.CloseStatus.Value,result.CloseStatusDescription,CancellationToken.None);
                                
                                Console.WriteLine("Received Close Message");
                                return;
                            }
                    });
                }
                else
                {
                    Console.WriteLine("Hello from the 2nd request delegate");
                    await _next(context);
                }
        }

        private async Task SendConnIDAsync(WebSocket socket, string connId)
        {
            var buffer = Encoding.UTF8.GetBytes($"ConnID: {connId}");
            await socket.SendAsync(buffer, WebSocketMessageType.Text,true,CancellationToken.None);
        }

        private async Task ReceiveMessage(WebSocket socket,Action<WebSocketReceiveResult,byte[]> handleMessage)
        {
           var buffer = new byte[1024*4];

           while(socket.State == WebSocketState.Open)
           {
               var result = await socket.ReceiveAsync(buffer:new ArraySegment<byte>(buffer),cancellationToken:CancellationToken.None);

               handleMessage(result,buffer);
           }
        }

         public void WriteRequestParam(HttpContext context)
        {
            Console.WriteLine("Request Method" + context.Request.Method);
             Console.WriteLine("Request protocal" + context.Request.Protocol);
             if(context.Request.Headers != null)
             {
                 Console.WriteLine("Request Headers ..." + context.Request.Protocol);
                 context.Request.Headers.ToList().ForEach (h=>{Console.WriteLine($"--> {h.Key} : {h.Value }");});                
             }
        }

        
        public async Task RouteJSONMessageAsync(string Message)
        {
               var routeOb = JsonConvert.DeserializeObject<WebSocketMsg>(Message);
            
               if (Guid.TryParse(routeOb.To,out Guid GuidOutput))
               {
                    Console.WriteLine("Targeted");
                    var sock = _manager.GetAllSockets().FirstOrDefault(s => s.Key == routeOb.To.ToString());
                   
                    await  SendTextMessage(sock, routeOb.Message.ToString());

               }
               else if (String.IsNullOrEmpty(routeOb.To))
               {
                Console.WriteLine($"BroadCast {routeOb.ToString()}");
                  foreach(var sock in _manager.GetAllSockets())
                  {
                      if(sock.Value.State == WebSocketState.Open)
                      {
                         string retMessage =  JsonConvert.SerializeObject(routeOb,Formatting.None);
                         var options = new JsonSerializerOptions
                        {
                            WriteIndented = false
                        };

                         var buffer = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(routeOb,options);
                          
                          await sock.Value.SendAsync(
                              buffer,
                              WebSocketMessageType.Text,
                              true,
                              CancellationToken.None);
                      }
                  }
               }
               else
               {
                   var sock = _manager.GetAllSockets().FirstOrDefault(s => s.Key == routeOb.From.ToString());
               }
        }

        private async Task SendTextMessage(KeyValuePair<string,WebSocket> sock,string Message )
        {
            if (sock.Value != null)
                    {
                        if(sock.Value.State == WebSocketState.Open)
                        {
                            await sock.Value.SendAsync(Encoding.UTF8.GetBytes(Message.ToString()),
                            WebSocketMessageType.Text,true,CancellationToken.None);
                        }
                        else
                        {
                            Console.WriteLine("Socket is Closed, invalid Recipient");
                        }
                    }
        }
    }
}
