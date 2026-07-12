# Baldur's Gate 3 Mod Manager Redux

BG3 Mod Manager Redux is a modernized, community-driven fork of
[LaughingLeader's BG3 Mod Manager](https://github.com/LaughingLeader/BG3ModManager).
Redux preserves the established mod-management backend while developing a cleaner interface,
safer file operations, richer metadata, and improved organization for large mod lists.

## Current version

**0.1.0-alpha.1 — private alpha**

This project is still a work in progress. Private-alpha builds are intended for careful personal
testing and a small group of invited testers. Features, metadata matching, themes, and interface
details may be incomplete or change between builds.

- Keep backups of important profiles and mod files.
- Verify an exported load order before launching the game.
- Stop and report unexpected behavior before repeating a destructive operation.
- Redux application self-updating is intentionally disabled during the private alpha.

No public Redux release is currently available from this repository.

## Redux highlights

- Modern Redux Dark, Redux Light, and Parchment color themes.
- Active and inactive mod management with preserved BG3 load-order behavior.
- Redux-only visual separators and collapsible organization sections.
- Persistent categories, custom category colors, filtering, and category ordering.
- Nexus Mods and mod.io metadata linking with local metadata fallback.
- A resizable selected-mod details drawer with descriptions, requirements, files, and changelogs.
- Clear handling for Override Mods, bundled Mod Fixer files, and mod.io safety limitations.
- Atomic settings and `modsettings.lsx` writes, staged imports, replacement backups, and safer deletion.
- A read-only Mod Health analysis foundation for future diagnostics.

## Building from source

Redux currently targets .NET 8 and Windows WPF. Open `BG3ModManager.sln` in Visual Studio with the
required .NET desktop and C++ build tools installed, then build the `x64` Debug or Release
configuration.

The internal assembly is still named `BG3ModManager` for compatibility with inherited WPF resource
paths. That internal name does not indicate an upstream release.

## Project links

- [Redux repository](https://github.com/raincloudsfollow/BG3ModManager-Redux)
- [Redux issue tracker](https://github.com/raincloudsfollow/BG3ModManager-Redux/issues)
- [Baldur's Gate 3 on Nexus Mods](https://www.nexusmods.com/baldursgate3)
- [BG3 Script Extender](https://github.com/Norbyte/bg3se)

## Upstream project and attribution

Redux exists because of LaughingLeader's original BG3 Mod Manager and retains substantial portions
of its code and core behavior. Upstream authorship and license notices must remain intact.

- [Original BG3 Mod Manager](https://github.com/LaughingLeader/BG3ModManager)
- [LaughingLeader](https://github.com/LaughingLeader)
- [Support LaughingLeader on Ko-fi](https://ko-fi.com/LaughingLeader)

Redux also depends on third-party projects including:

- [LSLib by Norbyte](https://github.com/Norbyte/lslib)
- [BG3 Script Extender by Norbyte](https://github.com/Norbyte/bg3se)
- CrossSpeak and its bundled screen-reader integrations
- Inter, distributed under the SIL Open Font License

See the repository license and bundled dependency licenses for complete terms and notices.

## License

The original project is provided under the MIT License. Redux modifications remain subject to that
license and all retained copyright notices. See [LICENSE](LICENSE).
