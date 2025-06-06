using QueueBot.Data.Types;
using Discord.WebSocket;
using QueueBot.Data.Storage;

namespace QueueBot.Managers
{
    public class QueueManager
    {
        private readonly DiscordSocketClient _client;
        private readonly Dictionary<(ulong GuildId, ulong ChannelId), Queue> _queues;

        public QueueManager(DiscordSocketClient client)
        {
            _client = client;
            _queues = new Dictionary<(ulong GuildId, ulong ChannelId), Queue>();
        }

        public async Task InitializeQueues()
        {
            var queueConfigs = await QueueStorageService.GetAllConfigsAsync();

            foreach (var config in queueConfigs)
            {
                var queue = new Queue(_client, config);
                _queues.Add((config.GuildId, config.ChannelId), queue);
                queue.InitializeQueue();
            }
        }

        public async Task InitializeQueue(QueueConfig config)
        {
            var queue = new Queue(_client, config);
            _queues.Add((config.GuildId, config.ChannelId), queue);
            queue.InitializeQueue();
        }

        public async Task EditQueue(QueueConfig config)
        {
            var queue = _queues.Values.Where(q => q.Config.ChannelId == config.ChannelId).FirstOrDefault();
            queue?.InitializeQueue();
        }

        public Queue GetQueue(ulong guildID, ulong channelID)
        {
            return _queues[(guildID, channelID)];
        }

        public Queue GetQueue(ulong channelID)
        {
            var guildId = (_client.GetChannel(channelID) as SocketGuildChannel).Guild.Id;
            return GetQueue(guildId, channelID);
        }
    }
}
