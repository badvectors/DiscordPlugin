// Compile-time stand-in for vatSys.exe, so the plugin can build without vatSys installed (e.g. in CI).
//
// It declares ONLY the members the plugin uses, with signatures copied from the real vatSys.exe
// (checked by reflection against vatSys 0.4.9305.35667). None of this code ever runs: at runtime
// the plugin binds to the real vatSys already loaded in the process.
//
// If you use a new vatSys API in the plugin, add it here with the exact same shape
// (field vs property, static vs instance, parameter types). Getting it wrong compiles fine but
// fails at runtime with MissingFieldException/MissingMethodException. To double-check, build
// against the real exe: dotnet build -p:VatSysExe="C:\Program Files (x86)\vatSys\bin\vatSys.exe"
#pragma warning disable CS0067 // events never used

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace vatsys.Plugin
{
    public interface IPlugin
    {
        string Name { get; }
        void OnFDRUpdate(FDP2.FDR updated);
        void OnRadarTrackUpdate(RDP.RadarTrack updated);
    }

    public enum CustomToolStripMenuItemWindowType { Main = 0, ASD = 1, ASMGCS = 2 }

    public enum CustomToolStripMenuItemCategory { Custom = 0, Settings = 1, Windows = 2, Maps = 3, Tools = 4, Messages = 5, Info = 6 }

    public class CustomToolStripMenuItem
    {
        public CustomToolStripMenuItem(CustomToolStripMenuItemWindowType window, CustomToolStripMenuItemCategory existingCategory, ToolStripItem item) => throw null;

        public ToolStripItem Item;
    }
}

namespace vatsys
{
    public static class MMI
    {
        public static void AddCustomMenuItem(Plugin.CustomToolStripMenuItem item) => throw null;
        public static void InvokeOnGUI(MethodInvoker method) => throw null;
    }

    public static class Errors
    {
        public static void Add(Exception ex, string source) => throw null;
    }

    public class Network
    {
        public static event EventHandler Connected;
        public static event EventHandler Disconnected;
        public static NetworkATC Me => throw null;
        public static bool IsOfficialServer => throw null;
    }

    public class NetworkATC
    {
        public string Callsign;
    }

    public class NetworkPilot
    {
        public string Callsign;
    }

    public class Audio
    {
        public static event EventHandler VSCSFrequenciesChanged;
        public static event EventHandler TransmittingChanged;
        public static bool IsAFVConnected => throw null;
        public static IList<VSCSFrequency> VSCSFrequencies => throw null;
        public static bool Transmitting => throw null;
    }

    public class VSCSFrequency
    {
        public event EventHandler TransmitChanged;
        public event EventHandler ReceiveChanged;
        public event EventHandler ReceivingChanged;
        public string Name => throw null;
        public string FriendlyName => throw null;
        public bool Receive { get => throw null; set => throw null; }
        public bool Transmit { get => throw null; set => throw null; }
        public bool Receiving => throw null;
    }

    public static class FDP2
    {
        public class FDR
        {
            public string Callsign { get => throw null; set => throw null; }
            public SectorsVolumes.Sector ControllingSector { get => throw null; set => throw null; }
        }
    }

    public static class RDP
    {
        public class RadarTrack
        {
            public NetworkPilot ActualAircraft { get => throw null; set => throw null; }
            public FDP2.FDR CoupledFDR { get => throw null; set => throw null; }
        }
    }

    public static class SectorsVolumes
    {
        public class Sector
        {
            public string Callsign;
        }
    }

    public static class Colours
    {
        public enum Identities { WindowBackground = 6, InteractiveText = 12 }

        public static Color GetColour(Identities id) => throw null;
    }

    // Real chain is BaseForm : NonClientBaseForm : Form. Dispose is declared here so a form's
    // base.Dispose(disposing) call binds to BaseForm.Dispose, as it does against the real exe.
    // Members here have working bodies so the Visual Studio forms designer can still open the window.
    public class BaseForm : Form
    {
        public bool Resizeable { get; set; }
        public bool HasMinimizeButton { get; set; }
        protected override void Dispose(bool disposing) => base.Dispose(disposing);
    }
}
