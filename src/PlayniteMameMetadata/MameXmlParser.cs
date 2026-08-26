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
                CloseInput = false
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
                    if (!string.IsNullOrWhiteSpace(machine.Name))
                    {
                        machines.Add(machine);
                    }
                }
            }

            return machines;
        }

        private static MameMachine ParseMachine(XElement element)
        {
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
    }
}
