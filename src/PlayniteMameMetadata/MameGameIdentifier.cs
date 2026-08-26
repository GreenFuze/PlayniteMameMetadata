using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PlayniteMameMetadata
{
    public sealed class MameGameIdentifier
    {
        private static readonly Regex CloudArchivePattern = new Regex(
            @"(?:^|\r?\n)Cloud archive:\s*(?<path>[^\r\n]+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public IEnumerable<string> GetCandidates(Game game)
        {
            if (game == null)
            {
                yield break;
            }

            var hasArcadePlatform = HasArcadePlatform(game);
            var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (game.Roms != null)
            {
                foreach (var rom in game.Roms)
                {
                    var romPath = rom?.Path ?? rom?.Name;
                    if (!hasArcadePlatform && !HasMamePathContext(romPath))
                    {
                        continue;
                    }

                    var candidate = Normalize(romPath);
                    if (candidate != null && emitted.Add(candidate))
                    {
                        yield return candidate;
                    }
                }
            }

            if (game.Links != null)
            {
                foreach (var link in game.Links)
                {
                    var url = link?.Url;
                    if (string.IsNullOrWhiteSpace(url))
                    {
                        continue;
                    }

                    Uri uri;
                    if (Uri.TryCreate(url, UriKind.Absolute, out uri) && IsArcadeMuseumHost(uri.Host))
                    {
                        var candidate = Normalize(uri.Segments.LastOrDefault());
                        if (candidate != null && emitted.Add(candidate))
                        {
                            yield return candidate;
                        }
                    }
                }
            }

            var match = CloudArchivePattern.Match(game.Description ?? string.Empty);
            if (match.Success)
            {
                var archivePath = match.Groups["path"].Value;
                if (hasArcadePlatform || HasMamePathContext(archivePath))
                {
                    var candidate = Normalize(archivePath);
                    if (candidate != null && emitted.Add(candidate))
                    {
                        yield return candidate;
                    }
                }
            }

            if (hasArcadePlatform)
            {
                var nameCandidate = Normalize(game.Name);
                if (nameCandidate != null && emitted.Add(nameCandidate))
                {
                    yield return nameCandidate;
                }
            }
        }

        public bool TryIdentify(Game game, MameIndex index, out Models.MameMachine machine)
        {
            machine = null;
            if (index == null)
            {
                return false;
            }

            foreach (var candidate in GetCandidates(game))
            {
                if (index.TryGet(candidate, out machine))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim().TrimEnd('/', '\\');
            normalized = normalized.Replace('/', Path.DirectorySeparatorChar);
            normalized = Path.GetFileName(normalized);
            try
            {
                normalized = Uri.UnescapeDataString(normalized ?? string.Empty);
            }
            catch (UriFormatException)
            {
                return null;
            }
            var extension = Path.GetExtension(normalized);
            if (!string.IsNullOrWhiteSpace(extension))
            {
                normalized = Path.GetFileNameWithoutExtension(normalized);
            }

            return string.IsNullOrWhiteSpace(normalized) ? null : normalized.Trim();
        }

        private static bool IsArcadeMuseumHost(string host)
        {
            return string.Equals(host, "arcade-museum.com", StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(host) &&
                 host.EndsWith(".arcade-museum.com", StringComparison.OrdinalIgnoreCase));
        }

        private static bool HasArcadePlatform(Game game)
        {
            return game.Platforms?.Any(platform =>
                string.Equals(platform?.SpecificationId, "arcade", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(platform?.Name, "Arcade", StringComparison.OrdinalIgnoreCase)) == true;
        }

        private static bool HasMamePathContext(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return Regex.Split(value, @"[\\/]+")
                .Any(segment =>
                    string.Equals(segment, "MAME", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(segment, "Arcade", StringComparison.OrdinalIgnoreCase));
        }
    }
}
