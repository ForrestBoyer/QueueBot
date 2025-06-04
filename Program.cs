using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Discord;
using Discord.WebSocket;
using QueueBot.Services;
using Microsoft.Extensions.Logging;
using QueueBot.Managers;

class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging => 
            {
                logging.ClearProviders();
            })
            .ConfigureServices((context, services) =>
            {
                // Add Discord client to DI container
                services.AddSingleton<DiscordSocketClient>(sp =>
                {
                    var config = new DiscordSocketConfig
                    {
                        GatewayIntents = GatewayIntents.Guilds |
                                         GatewayIntents.GuildMembers |
                                         GatewayIntents.GuildMessages |
                                         GatewayIntents.MessageContent |
                                         GatewayIntents.DirectMessages |
                                         GatewayIntents.GuildVoiceStates
                    };
                    return new DiscordSocketClient(config);
                });

                // Add DiscordBotService to DI container
                services.AddHostedService<QueueBotService>();
                services.AddSingleton<QueueManager>();
                services.AddSingleton<InteractionHandler>();
            })
            .Build();

        // Start the host
        await host.RunAsync();
    }
}
