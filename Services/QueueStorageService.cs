using System.Text.Json;
using QueueBot.Data.Types;
using Discord.WebSocket;

namespace QueueBot.Data.Storage
{
    public static class QueueStorageService
    {
        private static readonly string _filePath = "Data/Storage/queue-configs.json";
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        public static async Task<List<QueueConfig>> GetAllConfigsAsync()
        {
            var json = await File.ReadAllTextAsync(_filePath);
            var configs = JsonSerializer.Deserialize<List<QueueConfig>>(json, _jsonOptions)
                ?? new List<QueueConfig>();

            return configs;
        }

        public static async Task<QueueConfig> GetConfigAsync(ulong channelId)
        {
            var json = await File.ReadAllTextAsync(_filePath);
            var configs = JsonSerializer.Deserialize<List<QueueConfig>>(json, _jsonOptions)
                ?? new List<QueueConfig>();

            return configs.Where(c => c.ChannelId == channelId).First();
        }

        public static async Task SaveConfigAsync(QueueConfig config)
        {
            // Load existing configs
            var configs = await GetAllConfigsAsync();

            // Update or add config
            var existing = configs.Find(c => c.GuildId == config.GuildId && c.ChannelId == config.ChannelId);
            if (existing != null)
            {
                existing.SpeakingTime = config.SpeakingTime;
                existing.MessageId = config.MessageId;
            }
            else
            {
                configs.Add(config);
            }

            // Save back to file
            var json = JsonSerializer.Serialize(configs, _jsonOptions);
            await File.WriteAllTextAsync(_filePath, json);
        }

        public static async Task RemoveConfigAsync(ulong channelId)
        {
            // Load existing configs
            var configs = await GetAllConfigsAsync();

            // Find config and remove
            var existing = configs.Find(c => c.ChannelId == channelId);
            if (existing != null)
            {
                configs.Remove(existing);
            }

            // Save back to file
            var json = JsonSerializer.Serialize(configs, _jsonOptions);
            await File.WriteAllTextAsync(_filePath, json);
        }
    }
}
