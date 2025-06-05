using Discord;
using Discord.WebSocket;
using QueueBot.Data.Storage;
using QueueBot.Services;

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
        private string? _lastEmbedHash = null;

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

        public void InitializeQueue()
        {
            BeginUpdateQueueMessageTimer();
        }

        public string AddUserToQueue(SocketUser user)
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
                return "You were added to the queue";
            }
        }

        public async Task<string> RemoveUserFromQueue(SocketUser user)
        {
            // Currently speaking User
            if (_currentSpeaker == user)
            {
                _currentSpeaker = null;
                await UpdateMuteStates();
                return "Removed from queue";
            }
            // User currently in Queue
            else if (_queue.Contains(user))
            {
                _queue.Remove(user);
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
                await UpdateMuteStates();
            }
            else
            {
                // Re-enter previous speaker into the queue
                var previousSpeaker = _currentSpeaker;

                _currentSpeaker = _queue[0];
                _queue.RemoveAt(0);
                _timeLeft = _config.SpeakingTime;

                if (previousSpeaker is not null)
                {
                    AddUserToQueue(previousSpeaker);
                }

                await UpdateMuteStates();
                await RunTimer();
            }
        }

        public async Task MakeUserCurrentSpeaker(SocketUser user)
        {
            _currentSpeaker = user;
            _queue.Remove(user);
            _timeLeft = _config.SpeakingTime;
            _isTimerRunning = true;

            await UpdateMuteStates();
            await RunTimer();
        }

        public async Task RunTimer()
        {
            _isTimerRunning = true;

            while (_isTimerRunning && _timeLeft > 0)
            {
                await Task.Delay(1000);
                _timeLeft--;
            }

            if (_isTimerRunning)
            {
                _isTimerRunning = false;
                await StartNextSpeaker();
            }
        }

        public async Task BeginUpdateQueueMessageTimer()
        {
            while (true)
            {
                await UpdateQueueMessage();
                await Task.Delay(5000);
            }
        }

        public async Task UpdateQueueMessage()
        {

            // Build Message
            var embed = new EmbedBuilder()
                .WithTitle($"{Channel.Name} Queue")
                .WithColor(Color.DarkPurple)
                .AddField("Speaker", _currentSpeaker?.GlobalName ?? "_None_", inline: true)
                .AddField("Queue Count", _queue.Count.ToString(), inline: true)
                .AddField("Queue List",
                    _queue.Count > 0
                    ? string.Join("\n", _queue.Select((u, i) => $"**{i + 1}.** {u.GlobalName}"))
                    : "_Queue is currently empty_")
                .AddField("Time Left", _timeLeft == 0 ? "_No current speaker_" : _timeLeft)
                .Build();

            // Only update message if content has changed
            string newHash = embed.Title + string.Join("", embed.Fields.Select(f => f.Value));

            if (newHash == _lastEmbedHash)
                return;

            _lastEmbedHash = newHash;

            QueueBotService.LogAsync(new LogMessage(LogSeverity.Info, $"{Channel.Name}", "Updated queue message"));

            // Build buttons
            var componentBuilder = new ComponentBuilder()
                .WithButton("Join", "join-button", ButtonStyle.Success)
                .WithButton("Leave", "leave-button", ButtonStyle.Danger)
                .WithButton("Start", "start-button", ButtonStyle.Secondary);

            // Check if message still exists in channel
            var messageStillExists = false;
            if (_config.MessageId is not null)
            {
                messageStillExists = await Channel.GetMessageAsync(_config.MessageId.Value) != null;
            }

            // If message never or no longer exists, send it
            if (_config.MessageId is null || !messageStillExists)
            {
                var message = await Channel.SendMessageAsync(embed: embed, components: componentBuilder.Build());
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
                    msg.Embed = embed;
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
