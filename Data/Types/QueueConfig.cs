namespace QueueBot.Data.Types
{
    public class QueueConfig
    {
        public ulong GuildId { get; }
        public ulong ChannelId { get; }
        public ulong? MessageId { get; set; }
        public int SpeakingTime { get; set; }

        public QueueConfig(ulong guildId, ulong channelId, ulong? messageId, int speakingTime)
        {
            GuildId = guildId;
            ChannelId = channelId;
            MessageId = messageId;
            SpeakingTime = speakingTime;
        }
    }
}
