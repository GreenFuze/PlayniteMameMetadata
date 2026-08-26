using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PlayniteMameMetadata.Models
{
    [DataContract]
    public sealed class MameIndexDocument
    {
        [DataMember(Name = "version", Order = 1)]
        public string Version { get; set; }

        [DataMember(Name = "machines", Order = 2)]
        public List<MameMachine> Machines { get; set; } = new List<MameMachine>();
    }
}

