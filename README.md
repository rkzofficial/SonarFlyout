# Sonar Flyout

[![CI](https://github.com/rkzofficial/SonarFlyout/actions/workflows/ci.yml/badge.svg)](https://github.com/rkzofficial/SonarFlyout/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)

A tray mixer for SteelSeries Sonar on Windows 11. Click the icon to change Sonar's channel volumes and output devices without opening SteelSeries GG.

<img src="docs/images/flyout.png" width="300" alt="The Sonar Flyout panel above the taskbar" /> <img src="docs/images/per-channel.png" width="300" alt="Choosing an output device for a single channel" />

## Features

- Volume and mute for Master, Game, Chat, Media, Aux and Mic
- Switch every channel to another output device, or give each channel its own
- Pick the microphone Sonar uses
- The tray icon shows the master volume; scroll over it to change it
- Light and dark theme, 13 languages, optional start with Windows

## Requirements

Windows 11 and [SteelSeries GG](https://steelseries.com/gg) with Sonar in Classic mode.

## Install

Microsoft Store: coming soon. Until then, build it from source (see below).

## Usage

| Action | What it does |
| --- | --- |
| Click the tray icon | Open or close the mixer |
| Scroll over the tray icon | Raise or lower master volume |
| Click a device under **Output** | Send every channel to that device |
| Click the link icon next to **Output** | Switch between one shared output and per-channel outputs |
| Click a channel name (per-channel mode) | Choose that channel's device |
| Click a speaker or mic icon | Mute or unmute |
| Right-click the tray icon | Open SteelSeries GG, Start with Windows, Exit |

If the icon ends up in the hidden icons area (^), drag it onto the taskbar.

## How it works

GG runs a local web server for Sonar. Sonar Flyout reads its address from GG's config file and talks to it on `127.0.0.1`, using the same calls the GG app makes. SteelSeries doesn't document this API, so a GG update can break things; if that happens, [open an issue](https://github.com/rkzofficial/SonarFlyout/issues).

## Building

You need Developer Mode on, the .NET 10 SDK, Visual Studio 2022 or its Build Tools with "Desktop development with C++" (for Native AOT), and Windows SDK 10.0.26100. Then:

```powershell
.\tools\install-dev.ps1
```

This builds the MSIX, registers it and starts the app. `tools\make-assets.ps1` regenerates the icons and logos, and `tools\make-store-art.ps1` the Store images.

## Translations

UI strings are in `Strings\<language>\Resources.resw`. If a translation reads wrong in your language, a pull request is welcome.

## Privacy

Sonar Flyout collects no data. See [PRIVACY.md](PRIVACY.md).

## Disclaimer

Sonar Flyout is an independent project and is not affiliated with, endorsed by, or sponsored by SteelSeries. SteelSeries, SteelSeries GG and SteelSeries Sonar are trademarks of SteelSeries ApS.

## License

[MIT](LICENSE)
