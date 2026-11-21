# ShulkerRDK

[中文](README_zh.md) · [Documentation](https://docs.lipoly.ink/ShulkerRDK/)

*Modular low-code SDK & resource composer for Minecraft*
<p>
    <img src="https://lipoly.ink/assets/badges/humanity.svg" alt="This project was built by human" width="200">
    <img src="https://lipoly.ink/assets/badges/llmDisclosure.svg" alt="This project contains human-audited ai generated code" width="200">
</p>
## Overview

ShulkerRDK is a modular CLI toolkit for Minecraft resource pack and modpack development. It combines an interactive shell, a task scripting DSL (**Levitate Tasks**, `.lvt`), and loadable extensions so you can automate asset conversion, dependency tracking, packaging, and CI/CD builds without keeping bulky binaries in Git.

## Getting Started

### Using a Project Template

If you want a pre-configured setup out of the box, start from one of the templates:

- [Resource Pack Template (`ShulkerRDK.RPTemplate`)](https://github.com/LiPolymer/ShulkerRDK.RPTemplate)
- [Modpack Template (`ShulkerRDK.MPTemplate`)](https://github.com/LiPolymer/ShulkerRDK.MPTemplate)

### Standalone or Wrapper Scripts

- Download a prebuilt binary from [GitHub Releases](https://github.com/LiPolymer/ShulkerRDK/releases) or [GitLab Releases](https://gitlab.com/LiPolymer/ShulkerRDK/-/releases), or
- Copy the cross-platform wrapper scripts from [`scripts/`](scripts/) (`srdk`, `srdk.ps1`, `srdk.bat`) into your project root to fetch the binary automatically without committing executables to Git.

Run `./srdk` in an empty folder to initialize `./shulker/proj.json` interactively, or run tasks directly from the command line (e.g. `./srdk build` for CI/CD).

### Quick Example

Declare extensions in `./shulker/proj.json` (auto-fetched from GitHub / GitLab releases on startup):

```json
{
  "ProjectName": "My Pack",
  "Version": "1.0.0",
  "RootPath": "./src/",
  "OutPath": "./build/",
  "Extensions": [
    "gh:LiPolymer/ShulkerRDK#ShulkerRDK.ResourceMagick@B0.21"
  ]
}
```

Write a task in `./shulker/tasks/build.lvt` and run it with `build` (in the interactive prompt) or `./srdk build`:

```lvt
import shulker.magick

makeCleanup
makeCopy
psdCvt
makePkg
```

For full usage guides, see the [Documentation Site](https://docs.lipoly.ink/ShulkerRDK/).

## Extensions

Core functionality (`shulker.core` — project/version management, packaging, `.nfm` network files, and Levitate built-ins) ships inside the main binary. First-party extensions add specialized tooling:

| Extension | ID | Description |
| :--- | :--- | :--- |
| **ModrinthPSK** | `shulker.modrinth` | Track Modrinth files as lightweight `.mrf` references, update/lock versions, and export `modrinth.index.json` |
| **Prismarine** | `shulker.prismarine` | Next-gen modpack backend powered by TridentCore (`.prf.json` / `pref://`, Modrinth search, Trident/Modrinth/CurseForge export) |
| **ResourceMagick** | `shulker.magick` | PNG/PSD conversion, PBR map extraction (`_n`/`_s`), and Java resource pack version migration (`mrvc`) |
| **AsepriteExtractor** | `shulker.ase` | Convert `.aseprite` files to PNG with `#tag` multi-variant layer export |
| **ShulkerFFmpeg** | `shulker.ffmpeg` | Batch-convert audio files to OGG Vorbis (with optional mono downmixing) |
| **ShulkerRRT** | `shulker.rrt` | File watcher and live in-game resource pack reload trigger (paired with the [ShulkerRRT mod](https://modrinth.com/project/DhnASaqg)) |

## Building from Source

Requires the **.NET 10 SDK** (and initialized git submodules for `submodules/Trident.Net`):

```bash
git clone --recursive https://github.com/LiPolymer/ShulkerRDK.git
cd ShulkerRDK
dotnet build ShulkerRDK.slnx
```

## License

- This project (core and first-party extensions), except the wrapper scripts in `scripts/`, is licensed under **GPLv3** ([`LICENSE`](LICENSE)).
- Wrapper scripts in [`scripts/`](scripts/) are licensed under **Apache 2.0** ([`scripts/LICENSE`](scripts/LICENSE)).
- Resource pack version conversion rules/data in `ShulkerRDK.ResourceMagick` (`mrvc`) are adapted from [agentdid127/ResourcePackConverter](https://github.com/agentdid127/ResourcePackConverter) under the **MIT License** ([`ResourcePackConverter.LICENSE.txt`](ShulkerRDK.ResourceMagick/VersionConverter/ResourcePackConverter.LICENSE.txt)).
