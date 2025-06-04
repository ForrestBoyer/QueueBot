using System.Linq;
using Discord;
using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using QueueBot.Data.Storage;

namespace QueueBot.Data.Types
{
    public class Queue
    {
        private readonly DiscordSocketClient _client;
        private readonly List<SocketUser> _queue;
        private readonly QueueConfig _config;
        private SocketUser? _currentSpeaker;
        private bool _isTimerRunning;
        private int _timeLeft;
        private SocketVoiceChannel Channel { get; set; }

        public Queue(DiscordSocketClient client, QueueConfig config) 
        {
            _client = client;
            _queue = new List<SocketUser>();
            _config = config;
            _currentSpeaker = null;
            _isTimerRunning = false;
            _timeLeft = 0;
            Channel = _client.GetChannel(config.ChannelId) as SocketVoiceChannel;
        }

        public async Task InitializeQueue()
        {
            await UpdateQueueMessage();
        }

        public async Task<string> AddUserToQueue(SocketUser user)
        {
            if (!Channel.ConnectedUsers.Contains(user))
            {
                return "You must be in the channel to join the queue";
            }
            else if (_queue.Contains(user))
            {
                return "You are already in the queue";
            }
            else if (_currentSpeaker == user)
            {
                return "You are currently speaking";
            }
            else
            {
                _queue.Add(user);
                await UpdateQueueMessage();
                return "You were added to the queue";
            }
        }

        public async Task<string> RemoveUserFromQueue(SocketUser user)
        {
            // Currently speaking User
            if (_currentSpeaker == user)
            {
                _currentSpeaker = null;
                await UpdateQueueMessage();
                return "Removed from queue";
            }
            // User currently in Queue
            else if (_queue.Contains(user))
            {
                _queue.Remove(user);
                await UpdateQueueMessage();
                return "Removed from queue";
            }
            // User not currently speaking or in queue
            else
            {
                return "You are not in the queue and therefore cannot be removed";
            }
        }

        public async Task StartNextSpeaker()
        {
            // Queue is empty
            if (_queue.Count == 0)
            {
                _currentSpeaker = null;
                _isTimerRunning = false;
            }
            else
            {
                _currentSpeaker = _queue[0];
                _queue.RemoveAt(0);
                _timeLeft = _config.SpeakingTime;
                _isTimerRunning = true;

                await UpdateMuteStates();
                await RunTimer();
            }
        }

        public async Task RunTimer()
        {
            while (_isTimerRunning && _timeLeft > 0)
            {
                await Task.Delay(1000);
                _timeLeft--;

                await UpdateQueueMessage();
            }

            if (_isTimerRunning)
            {
                _isTimerRunning = false;
                await StartNextSpeaker();
            }
        }

        public async Task UpdateQueueMessage()
        {
            // Build Message
            var embedBuilder = new EmbedBuilder()
                .WithTitle("Current Queue:")
                .WithDescription(string.Join("\n", _queue.Select(u => u.GlobalName)))
                .WithColor(Color.Purple);

            var componentBuilder = new ComponentBuilder()
                .WithButton("Join", "join-button", ButtonStyle.Success)
                .WithButton("Leave", "leave-button", ButtonStyle.Danger);

            // Check if message still exists in channel
            var messageStillExists = await Channel.GetMessageAsync(_config.MessageId.Value) != null;

            // If message never or no longer exists, send it
            if (_config.MessageId is null || !messageStillExists)
            {
                var message = await Channel.SendMessageAsync(embed: embedBuilder.Build(), components: componentBuilder.Build());
                _config.MessageId = message.Id;
                await QueueStorage.SaveConfigAsync(_config);
            }
            // If message does exist, update it
            else
            {
                var id = _config.MessageId.Value;
                var message = await Channel.GetMessageAsync(id) as IUserMessage;
                await message.ModifyAsync(msg =>
                {
                    msg.Embed = embedBuilder.Build();
                    msg.Components = componentBuilder.Build();
                });
            }

        }

        public async Task UpdateMuteStates()
        {
            // Mute everyone except for current speaker
            foreach (var user in Channel.ConnectedUsers)
            {
                if (user == _currentSpeaker)
                {
                    await user.ModifyAsync(u => u.Mute = false);
                }
                else
                {
                    await user.ModifyAsync(u => u.Mute = true);
                }
            }
        }
    }
}
