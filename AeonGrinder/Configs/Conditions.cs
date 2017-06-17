﻿using System.Collections.Generic;
using System.Xml.Serialization;

namespace AeonGrinder.Configs
{
    using Data;

    public class Conditions
    {
        [XmlAttribute("Name")]
        public string SkillName { get; set; }

        [XmlArrayItem("Condition")]
        public List<Condition> ConditionsList { get; set; }
    }
}
