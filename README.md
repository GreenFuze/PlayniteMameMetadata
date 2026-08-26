<p align="center">
  <img src="src/PlayniteMameMetadata/Resources/mame-metadata.png" width="128" height="128" alt="MAME DAT Metadata icon">
</p>

<h1 align="center">MAME DAT Metadata for Playnite</h1>

<p align="center">
  Resolve MAME machine short names into useful Playnite metadata.
</p>

<p align="center">
  <a href="https://github.com/GreenFuze/PlayniteMameMetadata/releases"><img alt="GitHub release" src="https://img.shields.io/github/v/release/GreenFuze/PlayniteMameMetadata?display_name=tag"></a>
  <a href="LICENSE"><img alt="Apache-2.0 license" src="https://img.shields.io/badge/license-Apache--2.0-blue.svg"></a>
  <img alt="Playnite API 6.16" src="https://img.shields.io/badge/Playnite_API-6.16-00a4ef.svg">
</p>

MAME DAT Metadata is a focused Playnite metadata provider for arcade games whose
library names are MAME machine identifiers such as `arknoid2` or `puckman`. It
maps exact identifiers from the official MAME `-listxml` data into metadata that
other Playnite providers can use as a canonical game identity.

## Features

- Exact, case-insensitive MAME machine-name matching.
- Identification from Arcade-platform game names, MAME/Arcade ROM paths,
  trusted Arcade Museum machine links, and MAME/Arcade Cloud Storage paths.
- Canonical title, release year, manufacturer/publisher, Arcade platform, and
  an Arcade Museum technical-machine link.
- A compact local index generated from the latest official MAME release.
- Automatic version checks at Playnite startup and a manual **Update MAME DAT
  index** command.
- No ROM scanning, ROM downloads, accounts, telemetry, or developer-operated
  service.

Non-runnable entries and MAME device definitions are excluded from the local
index. Matching is exact and requires arcade/MAME context; the plugin does not
use fuzzy title guesses or claim unrelated games that merely share a short
name.

## Install

After the add-on is accepted into Playnite's add-on database, install **MAME
DAT Metadata** from `Main menu > Add-ons > Browse > Metadata Sources`.

For a manual installation, download the latest `.pext` from
[Releases](https://github.com/GreenFuze/PlayniteMameMetadata/releases), open it,
and let Playnite restart.

On the first Playnite start after installation, wait for the notification that
the MAME index is ready. The initial index must be downloaded before this source
can return metadata.

## Configure metadata download

In Playnite's metadata download settings, enable **MAME DAT Metadata** for Name,
Release Date, Publisher, Links, and Platform. Put it before general-purpose
providers for those fields when resolving MAME identifiers.

If **Download only missing metadata** is enabled, Playnite treats an existing
identifier such as `arknoid2` as an already populated Name and does not ask any
provider to replace it. Disable that option when canonicalizing existing ROM
names.

Playnite 10 can cache another metadata provider against the original identifier
during the same bulk pass. If media from another source is still missing after
the Name changes, run missing-metadata download once more. An identity-first
Playnite core improvement is the proper long-term fix; this plugin deliberately
does not couple itself to unrelated media providers.

## Network and local data

The plugin makes unauthenticated HTTPS requests to GitHub's public API to check
the latest `mamedev/mame` release and downloads that release's full `lx.zip`
asset when the local index is absent or outdated. The archive is deleted after
successful parsing. A compact `mame-index.json` remains in Playnite's extension
data directory.

No game-library data, ROM names, paths, account details, or identifiers are sent
by the plugin. The release check downloads the same public MAME asset regardless
of the user's library. Standard connection information, such as an IP address
and user agent, is necessarily visible to GitHub when making these requests.

## Scope and legal notes

The extension package contains no ROMs, MAME executable or source code, and no
MAME data set. It downloads and processes official MAME `-listxml` output on the
user's machine. MAME's own documentation describes this output as intended for
frontends and ROM-management tools.

MAME is a registered trademark of Gregory Ember. This independent project is
not affiliated with or endorsed by MAMEdev, Gregory Ember, Arcade Museum, or
Playnite. Third-party copyright, licensing, and trademark details are recorded
in [NOTICE](NOTICE).

## Development

Requirements:

- Windows and .NET Framework 4.6.2 tooling
- Playnite SDK 6.16.0
- Playnite Toolbox for creating release packages

```powershell
dotnet build .\src\PlayniteMameMetadata\PlayniteMameMetadata.csproj
dotnet run --project .\tests\PlayniteMameMetadata.Tests\PlayniteMameMetadata.Tests.csproj
```

The optional live integration test downloads the current official MAME DAT:

```powershell
dotnet run --project .\tests\PlayniteMameMetadata.Tests\PlayniteMameMetadata.Tests.csproj -- --integration
```

Create the release package with Playnite's official Toolbox:

```powershell
.\build-package.ps1 -ToolboxPath C:\path\to\Toolbox.exe
```

This implementation adapts the MAME DAT indexing design from
[My Games Anywhere](https://github.com/GreenFuze/MyGamesAnywhere).

## License

Licensed under the [Apache License 2.0](LICENSE). See [NOTICE](NOTICE) for
attributions and third-party information.
