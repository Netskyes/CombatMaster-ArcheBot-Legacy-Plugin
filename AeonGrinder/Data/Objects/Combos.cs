using System.Collections.Generic;
using System.Xml.Serialization;

namespace AeonGrinder.Data
{
    using Enums;

    public class Combos
    {
        [XmlAttribute("Trigger")]
        public string Name { get; set; }

        public List<Condition> Conditions { get; set; }

        [XmlArrayItem("Name")]
        public List<string> Skills { get; set; }

        public Combos()
        {
            Conditions = new List<Condition>();
            Skills = new List<string>();
        }
    }
}
