using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Writers;
using nhitomi.Database;
using Swashbuckle.AspNetCore.Swagger;

namespace nhitomi
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            using var host = CreateWebHostBuilder(args).Build();

            if (!HandleArgs(host, args))
            {
                await host.Services.GetService<StartupInitializer>().RunAsync();

                await host.RunAsync();
            }
        }

        static bool HandleArgs(IWebHost host, IEnumerable<string> args)
        {
            foreach (var arg in args)
            {
                switch (arg)
                {
                    // generates API specification
                    case "--generate-spec":
                        var options = host.Services.GetService<IOptionsMonitor<ServerOptions>>().CurrentValue;
                        var swagger = host.Services.GetService<ISwaggerProvider>();

                        swagger.GetSwagger("docs", options.PublicUrl, Startup.ApiBasePath).SerializeAsV3(new OpenApiJsonWriter(Console.Out));
                        return true;
                }
            }

            return false;
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder<Startup>(args)
                   .UseContentRoot(AppContext.BaseDirectory)
                   .UseWebRoot(Path.Combine(AppContext.BaseDirectory, "static"))
                   .ConfigureAppConfiguration(config => config.Add(new ElasticConfigurationSource()));
    }
}