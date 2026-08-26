using PlayniteMameMetadata.Models;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;

namespace PlayniteMameMetadata
{
    public sealed class MameIndexStore
    {
        private readonly string indexPath;

        public MameIndexStore(string dataDirectory)
        {
            if (string.IsNullOrWhiteSpace(dataDirectory))
            {
                throw new ArgumentException("A data directory is required.", nameof(dataDirectory));
            }

            indexPath = Path.Combine(dataDirectory, "mame-index.json");
        }

        public MameIndex Load()
        {
            if (!File.Exists(indexPath))
            {
                return null;
            }

            var serializer = new DataContractJsonSerializer(typeof(MameIndexDocument));
            using (var stream = File.OpenRead(indexPath))
            {
                var document = (MameIndexDocument)serializer.ReadObject(stream);
                return new MameIndex(document.Version, document.Machines);
            }
        }

        public void Save(MameIndex index)
        {
            if (index == null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            var directory = Path.GetDirectoryName(indexPath);
            Directory.CreateDirectory(directory);
            var stagingPath = indexPath + ".staging";
            var backupPath = indexPath + ".backup";
            var serializer = new DataContractJsonSerializer(typeof(MameIndexDocument));
            var document = new MameIndexDocument
            {
                Version = index.Version,
                Machines = index.GetMachines().OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase).ToList()
            };

            try
            {
                using (var stream = File.Create(stagingPath))
                {
                    serializer.WriteObject(stream, document);
                }

                if (File.Exists(indexPath))
                {
                    File.Replace(stagingPath, indexPath, backupPath, true);
                    File.Delete(backupPath);
                }
                else
                {
                    File.Move(stagingPath, indexPath);
                }
            }
            finally
            {
                if (File.Exists(stagingPath))
                {
                    File.Delete(stagingPath);
                }
            }
        }
    }
}

