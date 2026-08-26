using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteMameMetadata.Models;
using System.Collections.Generic;

namespace PlayniteMameMetadata
{
    public sealed class MameMetadataProvider : OnDemandMetadataProvider
    {
        private static readonly List<MetadataField> NoFields = new List<MetadataField>();
        private static readonly List<MetadataField> Fields = new List<MetadataField>
        {
            MetadataField.Name,
            MetadataField.ReleaseDate,
            MetadataField.Publishers,
            MetadataField.Links,
            MetadataField.Platform
        };

        private readonly MameMachine machine;

        public override List<MetadataField> AvailableFields => machine == null ? NoFields : Fields;

        public MameMetadataProvider(Game game, MameIndex index, MameGameIdentifier identifier)
        {
            identifier.TryIdentify(game, index, out machine);
        }

        public override string GetName(GetMetadataFieldArgs args)
        {
            return machine?.Description;
        }

        public override ReleaseDate? GetReleaseDate(GetMetadataFieldArgs args)
        {
            int year;
            return machine != null && int.TryParse(machine.Year, out year) && year >= 1 && year <= 9999
                ? new ReleaseDate(year)
                : (ReleaseDate?)null;
        }

        public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args)
        {
            if (!string.IsNullOrWhiteSpace(machine?.Manufacturer))
            {
                yield return new MetadataNameProperty(machine.Manufacturer);
            }
        }

        public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
        {
            if (machine != null)
            {
                yield return new Link("Arcade Museum", "https://www.arcade-museum.com/Machine/" + machine.Name);
            }
        }

        public override IEnumerable<MetadataProperty> GetPlatforms(GetMetadataFieldArgs args)
        {
            if (machine != null)
            {
                yield return new MetadataNameProperty("Arcade");
            }
        }
    }
}

