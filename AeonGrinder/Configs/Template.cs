using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace AeonGrinder.Configs
{
    using Data;

    [Serializable]
    public class Template
    {
        public string Name { get; set; }

        [XmlArrayItem("Name")]
        public List<string> Rotation { get; set; }

        [XmlArrayItem("Combo")]
        public List<Combos> Combos { get; set; }

        [XmlArrayItem("Name")]
        public List<string> BoostingBuffs { get; set; }

        [XmlArrayItem("Name")]
        public List<string> CombatBuffs { get; set; }

        [XmlArrayItem("Name")]
        public List<Condition> Conditions { get; set; }

        public Template()
        {
            Rotation = new List<string>();
            Combos = new List<Combos>();
            BoostingBuffs = new List<string>();
            CombatBuffs = new List<string>();
            Conditions = new List<Condition>();
        }
    }
}
