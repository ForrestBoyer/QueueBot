using Discord.WebSocket;
using Discord;
using QueueBot.Managers;
using Discord.Rest;
using System.Threading.Channels;

namespace QueueBot.Services
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly QueueManager _queueManager;

        public InteractionHandler(DiscordSocketClient client, QueueManager queueManager)
        {
            _client = client;
            _queueManager = queueManager;

            _client.InteractionCreated += HandleInteractionAsync;
            _client.UserVoiceStateUpdated += HandleUserLeftChannel;
        }

        public async Task HandleUserLeftChannel(SocketUser user, SocketVoiceState state1, SocketVoiceState state2)
        {
            if (state1.VoiceChannel is not null)
            {
                var queue = _queueManager.GetQueue(state1.VoiceChannel.Id);
                if (queue is not null)
                {
                    await queue.RemoveUserFromQueue(user);
                }
            }
        }

        public async Task HandleInteractionAsync(SocketInteraction interaction)
        {
            if (interaction is SocketMessageComponent messageComponent && messageComponent.Data.Type is ComponentType.Button)
            {
                var user = messageComponent.User;

                var message = "";
                switch (messageComponent.Data.CustomId)
                {
                    case "join-button":
                        message = await _queueManager.GetQueue(messageComponent.GuildId.Value, messageComponent.ChannelId.Value).AddUserToQueue(user);
                        break;
                    case "leave-button":
                         message = await _queueManager.GetQueue(messageComponent.GuildId.Value, messageComponent.ChannelId.Value).RemoveUserFromQueue(user);
                        break;
                    case "start-button":
                        await _queueManager.GetQueue(messageComponent.GuildId.Value, messageComponent.ChannelId.Value).StartNextSpeaker();
                        message = "Queue started";
                        break;
                    default:
                        break;
                }

                if (!interaction.HasResponded)
                {
                    await interaction.RespondAsync(message, ephemeral: true);
                }
            }
        }

    }
}
