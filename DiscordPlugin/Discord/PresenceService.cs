using System;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscordPlugin.Discord
{
    /// <summary>
    /// Background loop that keeps Discord's activity in sync with <see cref="Presence"/> values from a source.
    /// Handles reconnecting when Discord starts/restarts, and stays under Discord's rate limit (5 updates / 20s).
    /// </summary>
    internal sealed class PresenceService : IDisposable
    {
        public const string ClientId = "1002630602538889387";
        private const string LargeImageKey = "68678556";

        private static readonly TimeSpan Tick = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan MinUpdateInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ReconnectInterval = TimeSpan.FromSeconds(15);

        private readonly Func<Presence> _source;
        private readonly DiscordIpcClient _client = new DiscordIpcClient(ClientId);
        private readonly ManualResetEventSlim _stop = new ManualResetEventSlim();
        private readonly Thread _thread;

        private string _lastSent;
        private DateTime _lastSentUtc = DateTime.MinValue;
        private DateTime _lastConnectAttemptUtc = DateTime.MinValue;

        public PresenceService(Func<Presence> source)
        {
            _source = source;
            _thread = new Thread(Run) { IsBackground = true, Name = "Discord presence" };
        }

        public void Start() => _thread.Start();

        public void Dispose()
        {
            _stop.Set();
            _thread.Join(TimeSpan.FromSeconds(2));
            _client.Dispose();
        }

        private void Run()
        {
            while (!_stop.Wait(Tick))
            {
                try
                {
                    Step();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[DiscordPlugin] Presence update failed: {ex.Message}");
                    _client.Disconnect();
                }
            }
        }

        private void Step()
        {
            var now = DateTime.UtcNow;

            if (!_client.IsConnected)
            {
                _lastSent = null;
                if (now - _lastConnectAttemptUtc < ReconnectInterval) return;
                _lastConnectAttemptUtc = now;
                _client.TryConnect();
                return;
            }

            if (!_client.IsReady) return;

            var activity = ToActivity(_source());
            var json = activity?.ToString(Formatting.None) ?? "null";

            if (json == _lastSent) return;
            if (now - _lastSentUtc < MinUpdateInterval) return;

            _client.SetActivity(activity);
            _lastSent = json;
            _lastSentUtc = now;
        }

        private static JObject ToActivity(Presence presence)
        {
            if (presence == null) return null;

            var activity = new JObject
            {
                ["type"] = 0,
                ["instance"] = true,
                ["timestamps"] = new JObject { ["start"] = presence.StartUnixSeconds },
                ["assets"] = new JObject { ["large_image"] = LargeImageKey, ["large_text"] = "vatSys" },
            };

            // Discord rejects these fields if present but shorter than 2 characters.
            if (presence.Details?.Length >= 2) activity["details"] = presence.Details;
            if (presence.State?.Length >= 2) activity["state"] = presence.State;

            return activity;
        }
    }
}
