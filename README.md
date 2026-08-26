# MAME DAT Metadata for Playnite

A focused Playnite metadata provider that identifies arcade ROMs by their MAME
machine short name and maps the official MAME DAT fields into Playnite.

The first release provides:

- exact, case-insensitive MAME short-name matching (`arknoid2`, `puckman`, etc.);
- identification from Playnite ROM paths, game names, prior Arcade Museum links,
  and Cloud Storage archive paths;
- title, release year, manufacturer/publisher, Arcade platform, and Arcade Museum
  link metadata;
- a compact local index generated from the latest official MAME release;
- automatic background index updates and a manual **Update MAME DAT index** menu
  command.

The plugin does not bundle ROMs or a MAME data set. It downloads the official
full MAME DAT archive from the `mamedev/mame` GitHub releases and stores only a
locally generated lookup index in Playnite's extension data directory.

## Development

```powershell
dotnet build src/PlayniteMameMetadata/PlayniteMameMetadata.csproj
dotnet run --project tests/PlayniteMameMetadata.Tests/PlayniteMameMetadata.Tests.csproj
```

This implementation adapts the MAME DAT indexing design from
[My Games Anywhere](https://github.com/GreenFuze/MyGamesAnywhere).

## License

Apache License 2.0. See [LICENSE](LICENSE) and [NOTICE](NOTICE).
