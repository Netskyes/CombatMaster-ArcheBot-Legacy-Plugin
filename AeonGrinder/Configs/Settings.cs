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

        public string TemplateName = string.Empty;
        public string MapName = string.Empty;
        public string RunPluginName = string.Empty;
        public string FinalAction = string.Empty;

        public int FightRadius = 60;
        public int MinHitpoints = 40;
        public int MinMana = 30;

        [XmlArrayItem("Name")]
        public List<string> Targets { get; set; }

        [XmlArrayItem("Name")]
        public List<string> CleanItems { get; set; }
        

        public Settings()
        {
            Targets = new List<string>();
            CleanItems = new List<string>();
        }
    }
}
