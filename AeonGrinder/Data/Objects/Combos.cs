using System.Collections.Generic;
using System.Xml.Serialization;

namespace AeonGrinder.Data
{
    public class Combos
    {
        public Combos()
        {
            Init();
        }

        public Combos(string name, List<string> skills, List<string> triggers)
        {
            Name = name;
            Skills = skills;
            Triggers = triggers;

            Init();
        }


        [XmlAttribute("Trigger")]
        public string Name { get; set; }

        [XmlArrayItem("Name")]
        public List<string> Skills { get; set; }

        [XmlArrayItem("Name")]
        public List<string> Triggers { get; set; }

        private void Init()
        {
            Skills = new List<string>();
            Triggers = new List<string>();
        }
    }
}
