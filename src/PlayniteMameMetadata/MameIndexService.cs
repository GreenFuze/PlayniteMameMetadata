using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PlayniteMameMetadata
{
    public sealed class MameIndexUpdateResult
    {
        public bool Changed { get; set; }

        public MameIndex Index { get; set; }
    }

    public sealed class MameIndexService : IDisposable
    {
        private readonly string dataDirectory;
        private readonly MameIndexStore store;
        private readonly MameXmlParser parser;
        private readonly MameReleaseClient releaseClient;
        private readonly SemaphoreSlim updateLock = new SemaphoreSlim(1, 1);
        private volatile MameIndex current;

        public MameIndex Current => current;

        public MameIndexService(string dataDirectory)
            : this(dataDirectory, new MameIndexStore(dataDirectory), new MameXmlParser(), new MameReleaseClient())
        {
        }

        public MameIndexService(
            string dataDirectory,
            MameIndexStore store,
            MameXmlParser parser,
            MameReleaseClient releaseClient)
        {
            this.dataDirectory = dataDirectory ?? throw new ArgumentNullException(nameof(dataDirectory));
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.parser = parser ?? throw new ArgumentNullException(nameof(parser));
            this.releaseClient = releaseClient ?? throw new ArgumentNullException(nameof(releaseClient));
        }

        public MameIndex LoadCachedIndex()
        {
            current = store.Load();
            return current;
        }

        public async Task<MameIndexUpdateResult> UpdateAsync(
            bool force,
            IProgress<double> downloadProgress,
            Action<string> phaseChanged,
            CancellationToken cancellationToken)
        {
            await updateLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                phaseChanged?.Invoke("Checking the latest MAME release...");
                var asset = await releaseClient.GetLatestAssetAsync(cancellationToken).ConfigureAwait(false);
                if (!force && current != null && string.Equals(current.Version, asset.Version, StringComparison.OrdinalIgnoreCase))
                {
                    return new MameIndexUpdateResult { Changed = false, Index = current };
                }

                Directory.CreateDirectory(dataDirectory);
                var downloadPath = Path.Combine(dataDirectory, "mame-dat.download");
                try
                {
                    phaseChanged?.Invoke("Downloading the official MAME DAT...");
                    await releaseClient.DownloadAsync(asset, downloadPath, downloadProgress, cancellationToken).ConfigureAwait(false);

                    phaseChanged?.Invoke("Parsing the MAME machine index...");
                    MameIndex nextIndex;
                    using (var archive = ZipFile.OpenRead(downloadPath))
                    {
                        var xml = archive.Entries.FirstOrDefault(entry =>
                            !string.IsNullOrWhiteSpace(entry.Name) &&
                            entry.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) &&
                            (entry.Name.StartsWith("mame", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(entry.Name, "mame.xml", StringComparison.OrdinalIgnoreCase)));
                        if (xml == null)
                        {
                            throw new InvalidDataException("The MAME DAT archive does not contain its root XML file.");
                        }

                        using (var stream = xml.Open())
                        {
                            nextIndex = new MameIndex(asset.Version, parser.Parse(stream));
                        }
                    }

                    if (nextIndex.Count == 0)
                    {
                        throw new InvalidDataException("The MAME DAT did not contain any machines.");
                    }

                    phaseChanged?.Invoke("Saving the MAME machine index...");
                    store.Save(nextIndex);
                    current = nextIndex;
                    return new MameIndexUpdateResult { Changed = true, Index = nextIndex };
                }
                finally
                {
                    if (File.Exists(downloadPath))
                    {
                        File.Delete(downloadPath);
                    }
                }
            }
            finally
            {
                updateLock.Release();
            }
        }

        public void Dispose()
        {
            updateLock.Dispose();
            releaseClient.Dispose();
        }
    }
}
