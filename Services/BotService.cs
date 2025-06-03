using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace QueueBot.Services
{
    public class QueueBotService : IHostedService
    {
        private readonly DiscordSocketClient _client;
        private readonly IConfiguration _configuration;

        public QueueBotService(IConfiguration configuration, DiscordSocketClient socketClient)
        {
            _configuration = configuration;
            _client = socketClient;

            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
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
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log);
            return Task.CompletedTask;
        }
    }
}