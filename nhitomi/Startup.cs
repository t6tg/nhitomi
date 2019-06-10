using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nhitomi.Core;
using nhitomi.Discord;
using nhitomi.Http;
using nhitomi.Interactivity;
using Newtonsoft.Json;

namespace nhitomi
{
    public static class Startup
    {
        public static void Configure(HostBuilderContext host, IConfigurationBuilder config)
        {
            config
                .SetBasePath(host.HostingEnvironment.ContentRootPath)
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{host.HostingEnvironment.EnvironmentName}.json", true)
                .AddEnvironmentVariables();
        }

        public static void ConfigureServices(HostBuilderContext host, IServiceCollection services)
        {
            var settings = host.Configuration.Get<AppSettings>();

            // configuration
            services
                .Configure<AppSettings>(host.Configuration);

            // logging
            services
                .AddLogging(l => l.AddConfiguration(host.Configuration.GetSection("logging"))
                    .AddConsole()
                    .AddDebug());

            // database
            if (settings.UseDatabase)
            {
                services
                    .AddScoped<IDatabase>(s => s.GetRequiredService<nhitomiDbContext>())
                    .AddDbContextPool<nhitomiDbContext>(d => d
                        .UseMySql(host.Configuration.GetConnectionString("nhitomi")));
            }

            // discord services
            services
                .AddSingleton<DiscordService>()
                .AddSingleton<CommandExecutor>()
                .AddSingleton<GalleryUrlDetector>()
                .AddSingleton<InteractiveManager>()
                .AddSingleton<GuildSettingsCache>()
                .AddSingleton<MessageHandlerService>()
                .AddSingleton<ReactionHandlerService>()
                .AddSingleton<IHostedService, MessageHandlerService>(s => s.GetService<MessageHandlerService>())
                .AddSingleton<IHostedService, ReactionHandlerService>(s => s.GetService<ReactionHandlerService>())
                .AddHostedService<StatusUpdateService>()
                .AddHostedService<LogHandlerService>()
                .AddHostedService<GuildSettingsSyncService>()
                .AddHostedService<FeedChannelUpdateService>();

            // http server
            services
                .AddHostedService<HttpService>()
                .AddSingleton<ProxyHandler>();

            // other stuff
            services
                .AddHttpClient()
                .AddTransient<IHttpClient, HttpClientWrapper>()
                .AddTransient(s => JsonSerializer.Create(new nhitomiSerializerSettings()))
                .AddHostedService<ForcedGarbageCollector>();
        }
    }
}