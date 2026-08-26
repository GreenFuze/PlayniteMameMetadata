using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteMameMetadata.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace PlayniteMameMetadata.Tests
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                if (args.Length > 0 && string.Equals(args[0], "--integration", StringComparison.OrdinalIgnoreCase))
                {
                    return RunIntegration(args.Length > 1 ? args[1] : null);
                }

                ParserReadsMameMachineXml();
                ParserReadsLegacyDatafileXml();
                ParserSkipsNonRunnableDevices();
                ParserIgnoresMameDocumentTypeDefinition();
                IndexLookupIsCaseInsensitive();
                IdentifierUsesCloudArchiveAndRomPaths();
                IdentifierValidatesArcadeMuseumHosts();
                IdentifierRejectsUnscopedNameMatches();
                MetadataProviderMapsFields();
                IndexStoreRoundTrips();
                Console.WriteLine("All PlayniteMameMetadata tests passed.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static int RunIntegration(string dataDirectory)
        {
            var deleteAfter = string.IsNullOrWhiteSpace(dataDirectory);
            dataDirectory = dataDirectory ?? Path.Combine(
                Path.GetTempPath(),
                "PlayniteMameMetadataIntegration",
                Guid.NewGuid().ToString("N"));

            try
            {
                using (var service = new MameIndexService(dataDirectory))
                {
                    var result = service.UpdateAsync(
                        true,
                        new Progress<double>(value => Console.Write($"\rDownloading: {value:F1}%")),
                        phase => Console.WriteLine("\n" + phase),
                        System.Threading.CancellationToken.None).GetAwaiter().GetResult();
                    MameMachine machine;
                    True(result.Index.TryGet("arknoid2", out machine), "latest DAT contains arknoid2");
                    Console.WriteLine($"Resolved arknoid2 as '{machine.Description}'.");
                    Console.WriteLine($"MAME DAT {result.Index.Version}: {result.Index.Count:N0} machines indexed.");
                }

                return 0;
            }
            finally
            {
                if (deleteAfter && Directory.Exists(dataDirectory))
                {
                    Directory.Delete(dataDirectory, true);
                }
            }
        }

        private static void ParserReadsMameMachineXml()
        {
            const string xml = "<mame><machine name='arknoid2' cloneof='arknoid2u'><description>Arkanoid - Revenge of DOH</description><year>1987</year><manufacturer>Taito Corporation Japan</manufacturer><input players='2'/><display type='raster'/><driver status='good'/></machine></mame>";
            var machines = Parse(xml);
            Equal(1, machines.Length, "machine count");
            Equal("arknoid2", machines[0].Name, "short name");
            Equal("Arkanoid - Revenge of DOH", machines[0].Description, "description");
            Equal("2", machines[0].Players, "players");
        }

        private static void ParserReadsLegacyDatafileXml()
        {
            const string xml = "<datafile><game name='puckman'><description>Puck Man</description><year>1980</year></game></datafile>";
            Equal("puckman", Parse(xml).Single().Name, "legacy datafile game");
        }

        private static void ParserSkipsNonRunnableDevices()
        {
            const string xml = "<mame>" +
                "<machine name='z80' isdevice='yes'><description>Zilog Z80</description></machine>" +
                "<machine name='bios' runnable='no'><description>BIOS</description></machine>" +
                "<machine name='arknoid2'><description>Arkanoid - Revenge of DOH</description></machine>" +
                "</mame>";
            var machines = Parse(xml);
            Equal(1, machines.Length, "non-runnable device filter");
            Equal("arknoid2", machines[0].Name, "remaining runnable machine");
        }

        private static void ParserIgnoresMameDocumentTypeDefinition()
        {
            const string xml = "<!DOCTYPE mame [<!ELEMENT mame (machine*)>]><mame>" +
                "<machine name='arknoid2'><description>Arkanoid - Revenge of DOH</description></machine>" +
                "</mame>";
            Equal("arknoid2", Parse(xml).Single().Name, "MAME document type ignored");
        }

        private static void IndexLookupIsCaseInsensitive()
        {
            var index = TestIndex();
            MameMachine machine;
            True(index.TryGet("ARKNOID2", out machine), "case-insensitive lookup");
            Equal("Arkanoid - Revenge of DOH", machine.Description, "lookup description");
        }

        private static void IdentifierUsesCloudArchiveAndRomPaths()
        {
            var identifier = new MameGameIdentifier();
            var cloudGame = new Game("Unknown") { Description = "Imported by Cloud Storage.\nCloud archive: My Drive/Games/MAME/arknoid2.zip" };
            MameMachine machine;
            True(identifier.TryIdentify(cloudGame, TestIndex(), out machine), "cloud archive candidate");

            var romGame = new Game("Unknown")
            {
                Roms = new ObservableCollection<GameRom>
                {
                    new GameRom("Arkanoid", @"C:\Games\MAME\arknoid2.zip")
                }
            };
            True(identifier.TryIdentify(romGame, TestIndex(), out machine), "ROM path candidate");
        }

        private static void IdentifierValidatesArcadeMuseumHosts()
        {
            var identifier = new MameGameIdentifier();
            MameMachine machine;
            var trusted = new Game("Unknown")
            {
                Links = new ObservableCollection<Link>
                {
                    new Link("Machine", "https://www.arcade-museum.com/tech-center/machine/arknoid2")
                }
            };
            True(identifier.TryIdentify(trusted, TestIndex(), out machine), "trusted Arcade Museum host");

            var lookalike = new Game("Unknown")
            {
                Links = new ObservableCollection<Link>
                {
                    new Link("Machine", "https://arcade-museum.com.example.invalid/machine/arknoid2")
                }
            };
            True(!identifier.TryIdentify(lookalike, TestIndex(), out machine), "lookalike Arcade Museum host rejected");
        }

        private static void IdentifierRejectsUnscopedNameMatches()
        {
            var identifier = new MameGameIdentifier();
            MameMachine machine;
            True(
                !identifier.TryIdentify(new Game("arknoid2"), TestIndex(), out machine),
                "name-only match without Arcade or MAME context rejected");
        }

        private static void MetadataProviderMapsFields()
        {
            var game = new Game("arknoid2")
            {
                Description = "Imported game.\nCloud archive: My Drive/Games/MAME/arknoid2.zip"
            };
            var provider = new MameMetadataProvider(game, TestIndex(), new MameGameIdentifier());
            True(provider.AvailableFields.Contains(MetadataField.Name), "name field available");
            Equal("Arkanoid - Revenge of DOH", provider.GetName(null), "metadata title");
            Equal(1987, provider.GetReleaseDate(null).Value.Year, "metadata year");
            Equal("Taito Corporation Japan", ((MetadataNameProperty)provider.GetPublishers(null).Single()).Name, "publisher");
            Equal("arcade", ((MetadataSpecProperty)provider.GetPlatforms(null).Single()).Id, "platform specification");
            Equal(
                "https://www.arcade-museum.com/tech-center/machine/arknoid2",
                provider.GetLinks(null).Single().Url,
                "Arcade Museum link");
        }

        private static void IndexStoreRoundTrips()
        {
            var directory = Path.Combine(Path.GetTempPath(), "PlayniteMameMetadataTests", Guid.NewGuid().ToString("N"));
            try
            {
                var store = new MameIndexStore(directory);
                store.Save(TestIndex());
                var loaded = store.Load();
                Equal("test", loaded.Version, "stored version");
                Equal(1, loaded.Count, "stored machine count");
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        private static MameMachine[] Parse(string xml)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                return new MameXmlParser().Parse(stream).ToArray();
            }
        }

        private static MameIndex TestIndex()
        {
            return new MameIndex("test", new[]
            {
                new MameMachine
                {
                    Name = "arknoid2",
                    Description = "Arkanoid - Revenge of DOH",
                    Year = "1987",
                    Manufacturer = "Taito Corporation Japan"
                }
            });
        }

        private static void True(bool value, string message)
        {
            if (!value)
            {
                throw new InvalidOperationException("Assertion failed: " + message);
            }
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
            {
                throw new InvalidOperationException($"Assertion failed ({message}): expected '{expected}', got '{actual}'.");
            }
        }

    }
}
