using System.Linq;
using Discord.WebSocket;
using Discord;
using QueueBot.Managers;
using Discord.Rest;
using System.Threading.Channels;
using QueueBot.Data.Types;
using QueueBot.Data.Storage;

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
            if (state1.VoiceChannel is not null && state1.VoiceChannel != state2.VoiceChannel)
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
            // Button Interactions
            if (interaction is SocketMessageComponent messageComponent && messageComponent.Data.Type is ComponentType.Button)
            {
                var user = messageComponent.User;

                var message = "";
                switch (messageComponent.Data.CustomId)
                {
                    case "join-button":
                        message = _queueManager.GetQueue(messageComponent.GuildId.Value, messageComponent.ChannelId.Value).AddUserToQueue(user);
                        break;
                    case "leave-button":
                         message = await _queueManager.GetQueue(messageComponent.GuildId.Value, messageComponent.ChannelId.Value).RemoveUserFromQueue(user);
                        break;
                    default:
                        break;
                }

                if (!interaction.HasResponded)
                {
                    await interaction.RespondAsync(message, ephemeral: true);
                }
            }
            
            // Slash Command Interactions
            else if (interaction is SocketSlashCommand command)
            {
                switch (command.CommandName)
                {
                    case "configure-queue-channel":
                    {
                        var channel = command.Data.Options.Where(d => d.Name == "channel").FirstOrDefault()?.Value;
                        var speakingTimeValue = command.Data.Options.Where(d => d.Name == "speaking-time").FirstOrDefault()?.Value;
                        if (channel is SocketVoiceChannel c && speakingTimeValue is long speakingTime)
                        {
                            var config = await QueueStorageService.GetConfigAsync(c.Id);
                            config.SpeakingTime = (int)speakingTime;
                            await QueueStorageService.SaveConfigAsync(config);
                            await _queueManager.EditQueue(config);
                            await interaction.RespondAsync("Queue channel configured!", ephemeral: true);
                        }
                    }
                    break;

                    case "create-queue-channel":
                    {
                        var channel = command.Data.Options.Where(d => d.Name == "channel").FirstOrDefault()?.Value;
                        var speakingTimeValue = command.Data.Options.Where(d => d.Name == "speaking-time").FirstOrDefault()?.Value;
                        if (channel is SocketVoiceChannel c && speakingTimeValue is long speakingTime)
                        {
                            var config = new QueueConfig(command.GuildId.Value, c.Id, null, (int)speakingTime);
                            await QueueStorageService.SaveConfigAsync(config);
                            await _queueManager.InitializeQueue(config);
                            await interaction.RespondAsync("Queue channel configured!", ephemeral: true);
                        }
                    }
                    break;

                    case "remove-queue-channel":
                    {
                        var channel = command.Data.Options.Where(d => d.Name == "channel").FirstOrDefault()?.Value;
                        if (channel is SocketVoiceChannel ch)
                        {
                            var messageId = (await QueueStorageService.GetConfigAsync(ch.Id)).MessageId;
                            if (messageId is not null)
                            {
                                var m = messageId.Value;
                                await (_client.GetChannel(ch.Id) as SocketVoiceChannel).DeleteMessageAsync(m);
                            }
                            await QueueStorageService.RemoveConfigAsync(ch.Id);
                            await interaction.RespondAsync("Queue channel removed!", ephemeral: true);
                        }
                    }
                    break;

                    default:
                        break;
                }
            }
        }
    }
}
