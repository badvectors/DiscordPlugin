using System;
using System.Collections.Generic;
using System.Linq;
using DiscordPlugin.Discord;

namespace DiscordPlugin.Session
{
    /// <summary>Turns a session snapshot into the two lines shown in Discord.</summary>
    internal static class PresenceFormatter
    {
        private const int MaxFieldLength = 128;

        public static Presence Format(SessionSnapshot session, DisplayType displayType)
        {
            if (!session.Connected) return null;

            var transmitting = session.Frequencies.Where(f => f.Transmit).Select(f => f.Name).ToList();
            var receiving = session.Frequencies.Where(f => f.Receive && !f.Transmit).Select(f => f.Name).ToList();

            string details;
            string state;

            if (transmitting.Count > 0)
            {
                details = $"Controlling {JoinNames(transmitting)}.";
                state = Subtitle(session, displayType);
            }
            else if (receiving.Count > 0)
            {
                details = $"Observing {JoinNames(receiving)}.";
                state = Spotted(session.AircraftSpotted);
            }
            else if (!session.OfficialServer)
            {
                details = "In the sweat box.";
                state = null;
            }
            else
            {
                details = string.IsNullOrWhiteSpace(session.Callsign) ? null : $"Connected as {session.Callsign}.";
                state = null;
            }

            var start = new DateTimeOffset(DateTime.SpecifyKind(session.StartUtc.Value, DateTimeKind.Utc)).ToUnixTimeSeconds();
            return new Presence(Truncate(details), Truncate(state), start);
        }

        private static string Subtitle(SessionSnapshot s, DisplayType displayType)
        {
            switch (displayType)
            {
                case DisplayType.TxSent:
                    return Count(s.TxSent, "No transmissions sent.", "transmission sent.", "transmissions sent.");
                case DisplayType.TxReceived:
                    return Count(s.TxReceived, "No transmissions received.", "transmission received.", "transmissions received.");
                case DisplayType.TxBoth:
                    return $"Transmissions (Tx/Rx): {s.TxSent:N0}/{s.TxReceived:N0}";
                case DisplayType.AcftSpotted:
                    return Spotted(s.AircraftSpotted);
                case DisplayType.AcftControlled:
                    return s.AircraftControlled == 0 ? "No aircraft controlled." : $"{s.AircraftControlled:N0} aircraft controlled.";
                default:
                    return null;
            }
        }

        private static string Spotted(int count) =>
            count == 0 ? "No aircraft spotted." : $"{count:N0} aircraft spotted.";

        private static string Count(int count, string zero, string one, string many) =>
            count == 0 ? zero : count == 1 ? $"1 {one}" : $"{count:N0} {many}";

        /// <summary>"A", "A and B", "A, B and C".</summary>
        internal static string JoinNames(IReadOnlyList<string> names)
        {
            if (names.Count == 1) return names[0];
            return string.Join(", ", names.Take(names.Count - 1)) + " and " + names[names.Count - 1];
        }

        private static string Truncate(string value)
        {
            if (value == null || value.Length <= MaxFieldLength) return value;
            return value.Substring(0, MaxFieldLength - 1) + "…";
        }
    }
}
