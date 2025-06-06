using Discord;
using Discord.API;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using QueueBot.Managers;

namespace QueueBot.Services
{
    public class QueueBotService : IHostedService
    {
        private readonly DiscordSocketClient _client;
        private readonly IConfiguration _configuration;
        private readonly QueueManager _queueManager;
        private InteractionHandler _handler;

        public QueueBotService(IConfiguration configuration, DiscordSocketClient socketClient, QueueManager queueManager, InteractionHandler handler)
        {
            _configuration = configuration;
            _client = socketClient;
            _queueManager = queueManager;
            _handler = handler;

            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.InteractionCreated += _handler.HandleInteractionAsync;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var token = _configuration["BotTokens:QueueBot"] 
                ?? Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN")
                ?? throw new Exception("Bot token not found in configuration or environment variables.");

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _client.LogoutAsync();
            await _client.StopAsync();
        }

        private async Task ReadyAsync()
        {
            // Register commands
            var commandBuilders = new List<SlashCommandBuilder>()
            {
                new SlashCommandBuilder()
                    .WithName("configure-queue-channel")
                    .WithDescription("Edit the properties of a queue channel")
                    .WithDefaultMemberPermissions(GuildPermission.Administrator)
                    .AddOption("channel", ApplicationCommandOptionType.Channel, "Channel to edit queue properties", isRequired: true)
                    .AddOption("speaking-time", ApplicationCommandOptionType.Integer, "Time given to each speaker in the channel", isRequired: true),
                new SlashCommandBuilder()
                    .WithName("create-queue-channel")
                    .WithDescription("Add queue system to a channel")
                    .WithDefaultMemberPermissions(GuildPermission.Administrator)
                    .AddOption("channel", ApplicationCommandOptionType.Channel, "Channel to attach queue to", isRequired: true)
                    .AddOption("speaking-time", ApplicationCommandOptionType.Integer, "Time given to each speaker in the channel", isRequired: true),
                new SlashCommandBuilder()
                    .WithName("remove-queue-channel")
                    .WithDescription("Remove queue system from a channel")
                    .WithDefaultMemberPermissions(GuildPermission.Administrator)
                    .AddOption("channel", ApplicationCommandOptionType.Channel, "Channel to remove queue from", isRequired: true)
            };

            var guild = _client.GetGuild(992232275968270407);

            await guild.DeleteApplicationCommandsAsync();

            try 
            {
                foreach (var commandBuilder in commandBuilders)
                {
                    await guild.CreateApplicationCommandAsync(commandBuilder.Build());
                }
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
            }

            Console.WriteLine($"{_client.CurrentUser} is connected!");

            await _queueManager.InitializeQueues();
        }

        public static Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log);
            return Task.CompletedTask;
        }
    }
}
