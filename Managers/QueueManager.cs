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
            var queueConfigs = await QueueStorage.GetAllConfigsAsync();

            foreach (var config in queueConfigs)
            {
                var queue = new Queue(_client, config);
                _queues.Add((config.GuildId, config.ChannelId), queue);
                await queue.InitializeQueue();
            }
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
