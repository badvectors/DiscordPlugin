using System;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace DiscordPlugin
{
    /// <summary>User settings, persisted to Discord.json next to the plugin DLL.</summary>
    internal sealed class PluginSettings
    {
        private static string FilePath =>
            Path.Combine(Path.GetDirectoryName(typeof(PluginSettings).Assembly.Location) ?? ".", "Discord.json");

        public DisplayType DisplayType { get; set; } = DisplayType.AcftSpotted;

        public static PluginSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    return JsonConvert.DeserializeObject<PluginSettings>(File.ReadAllText(FilePath)) ?? new PluginSettings();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DiscordPlugin] Couldn't read settings: {ex.Message}");
            }

            return new PluginSettings();
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
            }
            catch (Exception ex)
            {
                // The plugin folder may be read-only (e.g. under Program Files); the setting still applies this session.
                Debug.WriteLine($"[DiscordPlugin] Couldn't save settings: {ex.Message}");
            }
        }
    }
}
