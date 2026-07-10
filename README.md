# BG3MM Redux

A modernized Baldur's Gate 3 mod manager focused on clarity, control, and a cleaner BG3-first modding workflow.

BG3MM Redux is a rework of the Baldur's Gate 3 Mod Manager experience. The goal is to keep the direct control and familiar BG3-focused workflow that players already value, while modernizing the interface, improving organization, and making the manager feel cleaner and more predictable to use.

> **Project status**
>
> BG3MM Redux is in active development. Features, screenshots, download links, and documentation may change as the project moves toward a stable release. Back up your load orders and BG3 configuration files before testing development builds.

## What is BG3MM Redux?

BG3MM Redux is a dedicated mod manager for **Baldur's Gate 3**.

It is being built for players who want more control than the in-game Mod Manager, less overhead than a general-purpose modding platform, and a cleaner way to manage BG3-specific load orders, profiles, metadata, and mod files.

Redux is based on the original BG3 Mod Manager project by **LaughingLeader** and keeps respect for the workflow that made that tool useful in the first place. This repository exists to develop the Redux version and adapt the experience around a more modern BG3 modding workflow.

## Why Redux?

BG3 has multiple ways to manage mods, but each one has tradeoffs.

- The **in-game Mod Manager** is convenient, but it requires launching the game and can make automatic changes that are frustrating when you are trying to keep a stable setup.
- **Vortex** is useful, especially for Nexus users, but it is a broad modding platform built for many games rather than a BG3-specific workflow.
- **Mod Organizer 2** is excellent for many games, but using it with BG3 can be awkward compared to games it was designed around.
- The original **BG3 Mod Manager** is still the foundation many BG3 players prefer, but the experience can be modernized further.

BG3MM Redux is meant to sit in that space: focused, direct, and built around the way BG3 players actually manage load orders.

## Current functionality

The project currently builds on the original BG3 Mod Manager feature set, including:

- Detecting BG3 game and profile paths
- Reading installed `.pak` mods
- Managing active and inactive mods
- Drag-and-drop load order organization
- Exporting the active load order to the game
- Updating `modsettings.lsx`
- Saving and loading external load order files
- Importing load orders from save files
- Viewing mod metadata, descriptions, authors, dependencies, and UUIDs
- Exporting load order information for sharing or troubleshooting
- Exporting selected mods to zip files
- Opening common BG3-related folders from shortcuts
- Light and dark theme support
- Mod author utilities such as extraction, UUID copying, custom tags, and version generation

## Redux goals

Redux is not just a rename. The goal is to improve the full experience of managing BG3 mods.

Planned and ongoing areas of focus include:

- A cleaner, more modern interface
- Clearer mod organization and category support
- Better visibility into load order state and mod metadata
- Safer profile and load order handling
- More predictable behavior with fewer surprise changes
- Improved workflows around Nexus, mod.io, and manually installed mods where appropriate
- Better guidance for new users without removing control from advanced users
- A BG3-first workflow that avoids unnecessary clutter

The priority is simple: **control without clutter**.

## Download

Redux builds will be published through this repository's **Releases** page when they are ready.

- [BG3MM Redux Releases](https://github.com/raincloudsfollow/BG3ModManager-Redux/releases)

Until a stable Redux release is available, assume builds are experimental. Avoid mixing multiple mod managers on the same setup unless you understand which tool is writing to your BG3 configuration.

## Basic setup

1. Run Baldur's Gate 3 at least once so the game creates the required profile and mod folders.
2. Install the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) if it is not already installed.
3. Install the latest [Microsoft Visual C++ Redistributable](https://aka.ms/vs/17/release/vc_redist.x64.exe).
4. Download a Redux build from this repository's Releases page when available.
5. Extract the application to a normal user folder. Do not extract it into `Program Files` or another protected Windows folder.
6. Launch the mod manager.
7. Confirm that the game data path and executable path are detected correctly. If they are not, set them manually in the preferences/settings window.
8. Organize your active mods, then export the load order to the game.

## Important notes

- Back up your load order before testing new builds.
- Avoid placing subfolders inside `%LOCALAPPDATA%\Larian Studios\Baldur's Gate 3\Mods`. BG3 expects mod `.pak` files directly in the Mods folder.
- Make sure the game data path points to the BG3 `Data` folder.
- If BG3 resets `modsettings.lsx`, one or more mods may be missing dependencies, outdated, incompatible, or failing to load.
- Be careful when switching between Redux, the in-game Mod Manager, Vortex, or other tools. More than one tool can modify the same BG3 load order files.

## Building from source

### Requirements

- Windows
- Visual Studio 2022
- .NET 8 SDK
- Desktop development workloads for WPF/.NET
- C++ build tools for native dependencies

### Clone

```bash
git clone --recursive https://github.com/raincloudsfollow/BG3ModManager-Redux.git
cd BG3ModManager-Redux
```

If the repository was cloned without submodules, initialize them with:

```bash
git submodule update --init --recursive
```

### Build

Open `BG3ModManager.sln` in Visual Studio and build the solution using an x64 configuration.

The solution includes the main GUI project, core mod manager logic, toolbox utilities, and external dependencies such as LSLib and CrossSpeak.

## Contributing

Feedback, bug reports, and contributions are welcome.

When opening an issue, include as much relevant information as possible:

- What you were trying to do
- What happened instead
- Whether the issue happens in BG3, Redux, or both
- Your Redux version or commit
- Your BG3 game version
- Any relevant logs, screenshots, or error messages

For pull requests, keep changes focused and explain what the change is intended to fix or improve.

## Credits

BG3MM Redux is based on the original **Baldur's Gate 3 Mod Manager** by [LaughingLeader](https://github.com/LaughingLeader).

Additional credit and thanks to:

- [Norbyte](https://github.com/Norbyte) for [LSLib](https://github.com/Norbyte/lslib)
- LaughingLeader for CrossSpeak and the original BG3 Mod Manager foundation
- The Baldur's Gate 3 modding community
- BG3 mod authors, testers, tool creators, and players who continue to improve the modding ecosystem
- [Larian Studios](https://larian.com/) for Baldur's Gate 3

## Disclaimer

BG3MM Redux is a community project and is not affiliated with, endorsed by, or officially supported by Larian Studios, Nexus Mods, mod.io, or Wizards of the Coast.

## License

This project follows the license included in this repository. See [`LICENSE`](LICENSE) for details.
