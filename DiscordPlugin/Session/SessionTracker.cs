using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordPlugin.Session
{
    internal sealed class FrequencyState
    {
        public FrequencyState(string name, bool receive, bool transmit)
        {
            Name = name;
            Receive = receive;
            Transmit = transmit;
        }

        public string Name { get; }
        public bool Receive { get; }
        public bool Transmit { get; }
    }

    /// <summary>Immutable copy of the session, safe to read from any thread.</summary>
    internal sealed class SessionSnapshot
    {
        public bool Connected => StartUtc.HasValue;
        public DateTime? StartUtc { get; set; }
        public string Callsign { get; set; }
        public bool OfficialServer { get; set; }
        public int TxSent { get; set; }
        public int TxReceived { get; set; }
        public int AircraftSpotted { get; set; }
        public int AircraftControlled { get; set; }
        public IReadOnlyList<FrequencyState> Frequencies { get; set; } = Array.Empty<FrequencyState>();
    }

    /// <summary>
    /// Collects session stats from vatSys events. vatSys raises these on several threads,
    /// so every member takes the same lock.
    /// </summary>
    internal sealed class SessionTracker
    {
        private readonly object _lock = new object();
        private readonly HashSet<string> _spotted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _controlled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private DateTime? _startUtc;
        private string _callsign;
        private bool _officialServer;
        private int _txSent;
        private int _txReceived;
        private List<FrequencyState> _frequencies = new List<FrequencyState>();

        public void Connect(string callsign, bool officialServer)
        {
            lock (_lock)
            {
                ResetCounters();
                _startUtc = DateTime.UtcNow;
                _callsign = callsign;
                _officialServer = officialServer;
            }
        }

        public void Disconnect()
        {
            lock (_lock)
            {
                ResetCounters();
                _startUtc = null;
                _callsign = null;
            }
        }

        public void AircraftSeen(string callsign, bool controlledByMe)
        {
            if (string.IsNullOrWhiteSpace(callsign)) return;

            lock (_lock)
            {
                if (!_startUtc.HasValue) return;
                _spotted.Add(callsign);
                if (controlledByMe) _controlled.Add(callsign);
            }
        }

        public void SetFrequencies(IEnumerable<FrequencyState> frequencies)
        {
            var list = frequencies
                .Where(f => !string.IsNullOrWhiteSpace(f.Name))
                .GroupBy(f => f.Name)
                .Select(g => g.First())
                .ToList();

            lock (_lock) _frequencies = list;
        }

        /// <summary>Counted only while transmitting on at least one frequency (i.e. controlling).</summary>
        public void TransmissionSent()
        {
            lock (_lock)
            {
                if (_frequencies.Any(f => f.Transmit)) _txSent++;
            }
        }

        /// <summary>Counted only while transmitting on at least one frequency (i.e. controlling).</summary>
        public void TransmissionReceived()
        {
            lock (_lock)
            {
                if (_frequencies.Any(f => f.Transmit)) _txReceived++;
            }
        }

        public SessionSnapshot Snapshot()
        {
            lock (_lock)
            {
                return new SessionSnapshot
                {
                    StartUtc = _startUtc,
                    Callsign = _callsign,
                    OfficialServer = _officialServer,
                    TxSent = _txSent,
                    TxReceived = _txReceived,
                    AircraftSpotted = _spotted.Count,
                    AircraftControlled = _controlled.Count,
                    Frequencies = _frequencies,
                };
            }
        }

        private void ResetCounters()
        {
            _spotted.Clear();
            _controlled.Clear();
            _txSent = 0;
            _txReceived = 0;
        }
    }
}
