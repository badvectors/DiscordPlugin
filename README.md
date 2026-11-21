# DiscordPlugin

A Discord presence plugin for [vatSys](https://virtualairtrafficsystem.com/).

#### Installation

Install using the [vatSys Launcher](https://github.com/badvectors/vatSysLauncher).

The plugin is a single DLL (`DiscordPlugin.Plugin.dll`). It talks to the Discord desktop app directly over its local IPC pipe, so there's no helper exe or native Discord SDK to install. If you're upgrading from 1.4 or earlier, you can delete `DiscordPlugin.Discord.exe`, `discord_game_sdk.d`, `discord_game_sdk.d.lib` and `DiscordPlugin.Common.dll` from the plugin folder.

#### How to use

The first line shows the frequencies you're transmitting on (controlling) or receiving on (observing).

The second line can be changed in vatSys under "Settings > Discord". The options are:
- Time Only: Only the time online will be displayed.
- Aircraft Spotted: The number of unique aircraft that have been spotted.
- Aircraft Controlled: The number of unique aircraft that have been in your jurisdiction. This means it won't work for TWR, GND and DEL positions.
- Transmissions Sent: The number of transmissions you have made.
- Transmissions Received: The number of transmissions you have received.
- Transmissions Both: The number of transmissions made and received.

Your choice is saved to `Discord.json` in the plugin folder.

#### Building

Requires the .NET SDK. vatSys doesn't need to be installed: the plugin compiles against a small stub of the vatSys API in [`ref/vatSys`](ref/vatSys/vatSys.cs), and binds to the real vatSys at runtime.

```
dotnet build -c Release
```

If you use a new vatSys API, add it to the stub with exactly the same shape as the real one. To check the stub still matches, build against a real install:

```
dotnet build -c Release -p:VatSysExe="C:\Program Files (x86)\vatSys\bin\vatSys.exe"
```

#### Releasing

Every push to `master` runs the [Release workflow](.github/workflows/release.yml). It builds the plugin and publishes a GitHub release tagged `v<Major>.<Minor>` from `<Version>` in `DiscordPlugin/DiscordPlugin.csproj`, with the DLL in `DiscordPlugin.zip`. If that release already exists, nothing is published. To cut a release, bump `<Version>` and `Version.json` together; the workflow fails if they don't match.
