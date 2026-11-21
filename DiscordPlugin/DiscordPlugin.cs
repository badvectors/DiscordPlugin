using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using DiscordPlugin.Discord;
using DiscordPlugin.Session;
using Newtonsoft.Json.Linq;
using vatsys;
using vatsys.Plugin;

namespace DiscordPlugin
{
    [Export(typeof(IPlugin))]
    public sealed class DiscordPlugin : IPlugin
    {
        private const string DisplayName = "Discord";
        private const string VersionUrl = "https://raw.githubusercontent.com/badvectors/DiscordPlugin/master/Version.json";

        private static readonly Version CurrentVersion = typeof(DiscordPlugin).Assembly.GetName().Version;

        private readonly SessionTracker _session = new SessionTracker();
        private readonly PresenceService _presence;
        private readonly List<VSCSFrequency> _subscribedFrequencies = new List<VSCSFrequency>();
        private readonly object _frequencyLock = new object();

        private DiscordWindow _window;

        internal static PluginSettings Settings { get; } = PluginSettings.Load();

        public string Name => DisplayName;

        public DiscordPlugin()
        {
            var menu = new CustomToolStripMenuItem(
                CustomToolStripMenuItemWindowType.Main,
                CustomToolStripMenuItemCategory.Settings,
                new ToolStripMenuItem(DisplayName));
            menu.Item.Click += (s, e) => ShowWindow();
            MMI.AddCustomMenuItem(menu);

            Network.Connected += (s, e) => _session.Connect(Network.Me?.Callsign, Network.IsOfficialServer);
            Network.Disconnected += (s, e) => _session.Disconnect();
            Audio.TransmittingChanged += Audio_TransmittingChanged;
            Audio.VSCSFrequenciesChanged += (s, e) => ResubscribeFrequencies();

            _presence = new PresenceService(() => PresenceFormatter.Format(_session.Snapshot(), Settings.DisplayType));
            _presence.Start();

            _ = CheckVersionAsync();
        }

        public void OnFDRUpdate(FDP2.FDR updated)
        {
            if (updated == null) return;
            _session.AircraftSeen(updated.Callsign, IsMine(updated));
        }

        public void OnRadarTrackUpdate(RDP.RadarTrack updated)
        {
            if (updated == null) return;
            var fdr = updated.CoupledFDR;
            _session.AircraftSeen(updated.ActualAircraft?.Callsign ?? fdr?.Callsign, fdr != null && IsMine(fdr));
        }

        private static bool IsMine(FDP2.FDR fdr)
        {
            var me = Network.Me?.Callsign;
            return me != null && fdr.ControllingSector?.Callsign == me;
        }

        private void Audio_TransmittingChanged(object sender, EventArgs e)
        {
            if (Audio.Transmitting && Audio.IsAFVConnected) _session.TransmissionSent();
        }

        private void Frequency_ReceivingChanged(object sender, EventArgs e)
        {
            // Fires on both start and end of a received transmission; count the start only.
            if (sender is VSCSFrequency freq && freq.Receiving) _session.TransmissionReceived();
        }

        private void Frequency_Changed(object sender, EventArgs e) => RefreshFrequencies();

        private void ResubscribeFrequencies()
        {
            lock (_frequencyLock)
            {
                foreach (var freq in _subscribedFrequencies)
                {
                    freq.ReceivingChanged -= Frequency_ReceivingChanged;
                    freq.TransmitChanged -= Frequency_Changed;
                    freq.ReceiveChanged -= Frequency_Changed;
                }
                _subscribedFrequencies.Clear();

                foreach (var freq in Audio.VSCSFrequencies)
                {
                    freq.ReceivingChanged += Frequency_ReceivingChanged;
                    freq.TransmitChanged += Frequency_Changed;
                    freq.ReceiveChanged += Frequency_Changed;
                    _subscribedFrequencies.Add(freq);
                }
            }

            RefreshFrequencies();
        }

        private void RefreshFrequencies()
        {
            _session.SetFrequencies(Audio.VSCSFrequencies.ToList().Select(f => new FrequencyState(
                string.IsNullOrWhiteSpace(f.FriendlyName) ? f.Name : f.FriendlyName,
                f.Receive,
                f.Transmit)));
        }

        private void ShowWindow()
        {
            MMI.InvokeOnGUI((MethodInvoker)delegate
            {
                if (_window == null || _window.IsDisposed) _window = new DiscordWindow();
                if (!_window.Visible) _window.Show();
                _window.Activate();
            });
        }

        private static async Task CheckVersionAsync()
        {
            try
            {
                using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
                {
                    var json = JObject.Parse(await http.GetStringAsync(VersionUrl).ConfigureAwait(false));
                    var latest = new Version((int)json["Major"], (int)json["Minor"]);
                    var current = new Version(CurrentVersion.Major, CurrentVersion.Minor);

                    if (latest > current)
                    {
                        Errors.Add(new Exception($"A new version of the plugin is available ({latest})."), DisplayName);
                    }
                }
            }
            catch
            {
                // Offline or GitHub unavailable; not worth bothering the user about.
            }
        }
    }
}
