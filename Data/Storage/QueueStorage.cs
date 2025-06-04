using System.Text.Json;
using QueueBot.Data.Types;

namespace QueueBot.Data.Storage
{
    public static class QueueStorage
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
    }
}
