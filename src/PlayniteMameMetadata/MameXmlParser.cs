using PlayniteMameMetadata.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace PlayniteMameMetadata
{
    public sealed class MameXmlParser
    {
        public IReadOnlyList<MameMachine> Parse(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var machines = new List<MameMachine>();
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                IgnoreComments = true,
                IgnoreWhitespace = true,
                CloseInput = false,
                XmlResolver = null
            };

            using (var reader = XmlReader.Create(stream, settings))
            {
                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element ||
                        (reader.LocalName != "machine" && reader.LocalName != "game"))
                    {
                        continue;
                    }

                    var machine = ParseMachine(XElement.Load(reader.ReadSubtree()));
                    if (machine != null && !string.IsNullOrWhiteSpace(machine.Name))
                    {
                        machines.Add(machine);
                    }
                }
            }

            return machines;
        }

        private static MameMachine ParseMachine(XElement element)
        {
            if (IsTrue((string)element.Attribute("isdevice")) ||
                IsFalse((string)element.Attribute("runnable")))
            {
                return null;
            }

            var machine = new MameMachine
            {
                Name = (string)element.Attribute("name"),
                CloneOf = (string)element.Attribute("cloneof"),
                Description = (string)element.Element("description"),
                Year = (string)element.Element("year"),
                Manufacturer = (string)element.Element("manufacturer"),
                Players = (string)element.Element("input")?.Attribute("players"),
                DriverStatus = (string)element.Element("driver")?.Attribute("status"),
                DisplayType = (string)element.Elements("display").FirstOrDefault()?.Attribute("type")
            };

            return machine;
        }

        private static bool IsTrue(string value)
        {
            return string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFalse(string value)
        {
            return string.Equals(value, "no", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "0", StringComparison.OrdinalIgnoreCase);
        }
    }
}
