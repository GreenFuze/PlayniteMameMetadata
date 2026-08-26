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
                    var asset = release.Assets?.FirstOrDefault(a =>
                        !string.IsNullOrWhiteSpace(a.Name) && a.Name.EndsWith("lx.zip", StringComparison.OrdinalIgnoreCase));
                    if (asset == null || string.IsNullOrWhiteSpace(asset.DownloadUrl))
                    {
                        throw new InvalidDataException("The latest MAME release has no full DAT (lx.zip) asset.");
                    }

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

            using (var response = await httpClient.GetAsync(asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength;
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
                        if (total.HasValue && total.Value > 0)
                        {
                            progress?.Report(copied * 100d / total.Value);
                        }
                    }
                }
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

