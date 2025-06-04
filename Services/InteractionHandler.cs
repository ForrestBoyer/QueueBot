using Discord.WebSocket;
using Discord;
using QueueBot.Managers;

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
                }

                await interaction.RespondAsync(message, ephemeral: true);
            }

            return;
        }

    }
}
