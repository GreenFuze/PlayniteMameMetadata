using System.Runtime.Serialization;

namespace PlayniteMameMetadata.Models
{
    [DataContract]
    public sealed class MameMachine
    {
        [DataMember(Name = "name", Order = 1)]
        public string Name { get; set; }

        [DataMember(Name = "cloneOf", Order = 2, EmitDefaultValue = false)]
        public string CloneOf { get; set; }

        [DataMember(Name = "description", Order = 3, EmitDefaultValue = false)]
        public string Description { get; set; }

        [DataMember(Name = "year", Order = 4, EmitDefaultValue = false)]
        public string Year { get; set; }

        [DataMember(Name = "manufacturer", Order = 5, EmitDefaultValue = false)]
        public string Manufacturer { get; set; }

        [DataMember(Name = "players", Order = 6, EmitDefaultValue = false)]
        public string Players { get; set; }

        [DataMember(Name = "driverStatus", Order = 7, EmitDefaultValue = false)]
        public string DriverStatus { get; set; }

        [DataMember(Name = "displayType", Order = 8, EmitDefaultValue = false)]
        public string DisplayType { get; set; }
    }
}

