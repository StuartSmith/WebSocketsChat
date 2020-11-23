using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.WebSockets;
using System.Threading;
using WebSocketServer.MiddleWare;

namespace WebSocketServer
{
    public class Startup
    {
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWebSocketManager();
        }


        
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            
           app.UseWebSockets();

           app.UseWebSocketServer();
           
           app.UseDefaultFiles();
            app.UseHttpsRedirection();
           app.UseStaticFiles();
           

           app.Run(async context =>
           {
                //WriteRequestParam(context);               
                await context.Response.WriteAsync("Hello from the 3rd request delegate");
           });
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

     

    }
}
