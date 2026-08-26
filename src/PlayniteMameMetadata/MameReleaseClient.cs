using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PlayniteMameMetadata
{
    public sealed class MameReleaseAsset
    {
        public string Version { get; set; }

        public string Name { get; set; }

        public string DownloadUrl { get; set; }
    }

    public sealed class MameReleaseClient : IDisposable
    {
        private const string LatestReleaseUrl = "https://api.github.com/repos/mamedev/mame/releases/latest";
        private const long MaximumDownloadBytes = 512L * 1024 * 1024;
        private readonly HttpClient httpClient;
        private readonly bool ownsClient;

        public MameReleaseClient(HttpClient httpClient = null)
        {
            this.httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            ownsClient = httpClient == null;
            if (!this.httpClient.DefaultRequestHeaders.UserAgent.Any())
            {
                this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PlayniteMameMetadata/0.1");
            }

            if (!this.httpClient.DefaultRequestHeaders.Accept.Any())
            {
                this.httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
            }
        }

        public async Task<MameReleaseAsset> GetLatestAssetAsync(CancellationToken cancellationToken)
        {
            using (var response = await httpClient.GetAsync(LatestReleaseUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    var serializer = new DataContractJsonSerializer(typeof(GitHubRelease));
                    var release = (GitHubRelease)serializer.ReadObject(stream);
                    if (release == null || string.IsNullOrWhiteSpace(release.TagName))
                    {
                        throw new InvalidDataException("The latest MAME release response has no version tag.");
                    }

                    var asset = release.Assets?.FirstOrDefault(a =>
                        !string.IsNullOrWhiteSpace(a.Name) &&
                        a.Name.StartsWith("mame", StringComparison.OrdinalIgnoreCase) &&
                        a.Name.EndsWith("lx.zip", StringComparison.OrdinalIgnoreCase));
                    if (asset == null || string.IsNullOrWhiteSpace(asset.DownloadUrl))
                    {
                        throw new InvalidDataException("The latest MAME release has no full DAT (lx.zip) asset.");
                    }

                    ValidateDownloadUrl(asset.DownloadUrl);

                    return new MameReleaseAsset
                    {
                        Version = release.TagName,
                        Name = asset.Name,
                        DownloadUrl = asset.DownloadUrl
                    };
                }
            }
        }

        public async Task DownloadAsync(
            MameReleaseAsset asset,
            string destinationPath,
            IProgress<double> progress,
            CancellationToken cancellationToken)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            ValidateDownloadUrl(asset.DownloadUrl);

            using (var response = await httpClient.GetAsync(asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength;
                if (total.HasValue && total.Value > MaximumDownloadBytes)
                {
                    throw new InvalidDataException("The MAME DAT download is larger than the supported safety limit.");
                }

                using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var output = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                {
                    var buffer = new byte[81920];
                    long copied = 0;
                    int read;
                    while ((read = await input.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        await output.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                        copied += read;
                        if (copied > MaximumDownloadBytes)
                        {
                            throw new InvalidDataException("The MAME DAT download exceeded the supported safety limit.");
                        }

                        if (total.HasValue && total.Value > 0)
                        {
                            progress?.Report(copied * 100d / total.Value);
                        }
                    }
                }
            }
        }

        private static void ValidateDownloadUrl(string value)
        {
            Uri uri;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase) ||
                !uri.AbsolutePath.StartsWith("/mamedev/mame/releases/download/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The MAME release contains an unexpected download URL.");
            }
        }

        public void Dispose()
        {
            if (ownsClient)
            {
                httpClient.Dispose();
            }
        }

        [DataContract]
        private sealed class GitHubRelease
        {
            [DataMember(Name = "tag_name")]
            public string TagName { get; set; }

            [DataMember(Name = "assets")]
            public List<GitHubAsset> Assets { get; set; }
        }

        [DataContract]
        private sealed class GitHubAsset
        {
            [DataMember(Name = "name")]
            public string Name { get; set; }

            [DataMember(Name = "browser_download_url")]
            public string DownloadUrl { get; set; }
        }
    }
}
