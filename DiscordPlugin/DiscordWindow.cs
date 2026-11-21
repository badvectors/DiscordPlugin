using System;
using System.Collections.Generic;
using System.Linq;
using vatsys;

namespace DiscordPlugin
{
    public partial class DiscordWindow : BaseForm
    {
        private static readonly IReadOnlyList<KeyValuePair<string, DisplayType>> Options = new[]
        {
            new KeyValuePair<string, DisplayType>("Transmissions Sent", DisplayType.TxSent),
            new KeyValuePair<string, DisplayType>("Transmissions Received", DisplayType.TxReceived),
            new KeyValuePair<string, DisplayType>("Transmissions Both", DisplayType.TxBoth),
            new KeyValuePair<string, DisplayType>("Aircraft Controlled", DisplayType.AcftControlled),
            new KeyValuePair<string, DisplayType>("Aircraft Spotted", DisplayType.AcftSpotted),
            new KeyValuePair<string, DisplayType>("Time Only", DisplayType.None),
        };

        private bool _loading;

        public DiscordWindow()
        {
            InitializeComponent();

            BackColor = Colours.GetColour(Colours.Identities.WindowBackground);
            ForeColor = Colours.GetColour(Colours.Identities.InteractiveText);

            comboBoxDisplay.Items.Clear();
            comboBoxDisplay.Items.AddRange(Options.Select(o => (object)o.Key).ToArray());
        }

        private void DiscordWindow_Load(object sender, EventArgs e)
        {
            _loading = true;
            var index = Options.ToList().FindIndex(o => o.Value == DiscordPlugin.Settings.DisplayType);
            comboBoxDisplay.SelectedIndex = index >= 0 ? index : -1;
            _loading = false;
        }

        private void comboBoxDisplay_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loading || comboBoxDisplay.SelectedIndex < 0) return;

            DiscordPlugin.Settings.DisplayType = Options[comboBoxDisplay.SelectedIndex].Value;
            DiscordPlugin.Settings.Save();
        }
    }
}
