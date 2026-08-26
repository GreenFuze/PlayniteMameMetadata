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

            var emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (game.Roms != null)
            {
                foreach (var rom in game.Roms)
                {
                    var candidate = Normalize(rom?.Path ?? rom?.Name);
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
                    if (Uri.TryCreate(url, UriKind.Absolute, out uri) &&
                        uri.Host.IndexOf("arcade-museum.com", StringComparison.OrdinalIgnoreCase) >= 0)
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
                var candidate = Normalize(match.Groups["path"].Value);
                if (candidate != null && emitted.Add(candidate))
                {
                    yield return candidate;
                }
            }

            var nameCandidate = Normalize(game.Name);
            if (nameCandidate != null && emitted.Add(nameCandidate))
            {
                yield return nameCandidate;
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
            normalized = Uri.UnescapeDataString(normalized ?? string.Empty);
            var extension = Path.GetExtension(normalized);
            if (!string.IsNullOrWhiteSpace(extension))
            {
                normalized = Path.GetFileNameWithoutExtension(normalized);
            }

            return string.IsNullOrWhiteSpace(normalized) ? null : normalized.Trim();
        }
    }
}

