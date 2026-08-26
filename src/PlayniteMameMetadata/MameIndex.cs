using PlayniteMameMetadata.Models;
using System;
using System.Collections.Generic;

namespace PlayniteMameMetadata
{
    public sealed class MameIndex
    {
        private readonly Dictionary<string, MameMachine> machines;

        public string Version { get; }

        public int Count => machines.Count;

        public MameIndex(string version, IEnumerable<MameMachine> source)
        {
            Version = version ?? string.Empty;
            machines = new Dictionary<string, MameMachine>(StringComparer.OrdinalIgnoreCase);

            if (source == null)
            {
                return;
            }

            foreach (var machine in source)
            {
                if (!string.IsNullOrWhiteSpace(machine?.Name))
                {
                    machines[machine.Name] = machine;
                }
            }
        }

        public bool TryGet(string shortName, out MameMachine machine)
        {
            machine = null;
            return !string.IsNullOrWhiteSpace(shortName) && machines.TryGetValue(shortName, out machine);
        }

        public IReadOnlyCollection<MameMachine> GetMachines()
        {
            return machines.Values;
        }
    }
}

