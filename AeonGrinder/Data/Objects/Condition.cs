using System.Xml.Serialization;

namespace AeonGrinder.Data
{
    using Enums;

    public class Condition
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("Value")]
        public string Value { get; set; }

        [XmlAttribute("Type")]
        public ConditionType Type { get; set; }
    }
}
