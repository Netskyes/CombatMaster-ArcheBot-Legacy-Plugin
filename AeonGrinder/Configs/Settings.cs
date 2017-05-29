using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace AeonGrinder.Configs
{
    [Serializable]
    public class Settings
    {
        public bool AutoStart;
        public bool LootTargets;
        public bool RunPlugin;
        public bool UseMeditate;
        public bool UsePlayDead;
        public bool LevelFamiliars;

        public string TemplateName = string.Empty;
        public string MapName = string.Empty;
        public string RunPluginName = string.Empty;
        public string FinalAction = string.Empty;
        public string FamiliarName = string.Empty;

        public int FightRadius = 60;
        public int MinHitpoints = 40;
        public int MinMana = 30;

        [XmlArrayItem("Name")]
        public List<string> Targets { get; set; }

        [XmlArrayItem("Name")]
        public List<string> CleanItems { get; set; }
        
        [XmlArrayItem("Name")]
        public List<string> HpRecoverItems { get; set; }

        [XmlArrayItem("Name")]
        public List<string> ManaRecoverItems { get; set; }

        [XmlArrayItem("Name")]
        public List<string> CombatBoosts { get; set; }

        public Settings()
        {
            Targets = new List<string>();
            CleanItems = new List<string>();
            HpRecoverItems = new List<string>();
            ManaRecoverItems = new List<string>();
            CombatBoosts = new List<string>();
        }
    }
}
