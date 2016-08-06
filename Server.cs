using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;

namespace Server
{
    public class Server
    {
        public static void Main(string[] args)
        {       
            var config = new ConfigurationBuilder()
                                .AddEnvironmentVariables()
                                .Build();

            var port = config["PORT"] ?? "3000";

            string serverUrl = "http://localhost:" + port;

            var host = new WebHostBuilder()
                            .UseUrls(serverUrl)
                            .UseKestrel()
                            .UseContentRoot(Directory.GetCurrentDirectory())
                            .UseStartup<Startup>()
                            .Build();

            host.Run();
        }
    }

    public class ResponseHeaderMiddleware 
    {
        private readonly RequestDelegate _next;
        public ResponseHeaderMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Response.OnStarting(state =>
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                    context.Response.Headers.Add("Cache-Control", "no-cache");

                    return Task.FromResult(0);

                }, null);

            await _next.Invoke(context);
        }
    }

    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        { 
            loggerFactory.AddConsole(LogLevel.Warning);

            //add custom headers
            app.UseMiddleware<ResponseHeaderMiddleware>();

            app.UseMvc();
        }
    }


    [Route("/api")]
    public class ApiController : Controller
    {
        private const string COMMENTS_FILE = "comments.json";

        private readonly IHostingEnvironment hostingEnv;

        public ApiController(IHostingEnvironment env)
        {
            hostingEnv = env;
        }


        [HttpGet("comments")]
        public IActionResult Comments()
        {  
            var fileData = System.IO.File.ReadAllText(Path.Combine(hostingEnv.ContentRootPath, COMMENTS_FILE));
            var json = JsonConvert.DeserializeObject(fileData);
            return Ok(json);
        }

        [HttpPost("comments")]
        public IActionResult Comments([FromBody] dynamic data)
        {  
            JObject newComment = JObject.FromObject(data);
            newComment["id"] = JToken.FromObject(DateTime.Now.Ticks);

            var fileData = System.IO.File.ReadAllText(Path.Combine(hostingEnv.ContentRootPath, COMMENTS_FILE));

            var comments = (JArray)JsonConvert.DeserializeObject(fileData);
            comments.Add(newComment);

            System.IO.File.WriteAllText(Path.Combine(hostingEnv.ContentRootPath, COMMENTS_FILE),
                                         JsonConvert.SerializeObject(comments, Formatting.Indented));

            return Ok(comments);
        }    
    }
}
