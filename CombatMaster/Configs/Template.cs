﻿using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace CombatMaster.Configs
{
    using Data;

    [Serializable]
    public class Template
    {
        public string Name { get; set; }
        public int MinHpHeals = 35;

        [XmlArrayItem("Name")]
        public List<string> Rotation { get; set; }

        [XmlArrayItem("Combo")]
        public List<Combos> Combos { get; set; }

        [XmlArrayItem("Heals")]
        public List<string> Heals { get; set; }

        [XmlArrayItem("Name")]
        public List<string> BoostingBuffs { get; set; }

        [XmlArrayItem("Name")]
        public List<string> CombatBuffs { get; set; }

        [XmlArrayItem("Skill")]
        public List<Conditions> CastConditions { get; set; }

        public Template()
        {
            Rotation = new List<string>();
            Combos = new List<Combos>();
            Heals = new List<string>();
            BoostingBuffs = new List<string>();
            CombatBuffs = new List<string>();
            CastConditions = new List<Conditions>();
        }
    }
}
