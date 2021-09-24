using System;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Gateway;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using StatusBot.Services;

namespace StatusBot
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .UseSerilog()
                .ConfigureServices(ConfigureServices)
                .ConfigureDiscordBot((context, bot) =>
                {
                    bot.Token = context.Configuration["Discord:Token"];
                    bot.ReadyEventDelayMode = ReadyEventDelayMode.Guilds;
                    bot.Intents += GatewayIntent.Members;
                    bot.Intents += GatewayIntent.Presences;
                    bot.Prefixes = context.Configuration.GetSection("Discord:Prefixes").Get<string[]>();
                    bot.UseMentionPrefix = true;
                })
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(host.Services.GetRequiredService<IConfiguration>())
                .CreateLogger();

            try
            {
                Log.Information("Running host");
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application crashed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddSingleton<StatusRecorderService>();
        }
    }
}
