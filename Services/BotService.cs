using Discord;
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
