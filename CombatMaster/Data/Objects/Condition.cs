using System.Xml.Serialization;

namespace CombatMaster.Data
{
    using Enums;

    public class Condition
    {
        public Condition()
        {
        }

        public Condition(string name, string value, ConditionType type)
        {
            Name = name;
            Value = value;
            Type = type;
        }

        
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [XmlAttribute("Value")]
        public string Value { get; set; }

        [XmlAttribute("Type")]
        public ConditionType Type { get; set; }
    }
}
