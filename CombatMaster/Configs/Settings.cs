using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace CombatMaster.Configs
{
    [Serializable]
    public class Settings
    {
        public bool AutoStart;
        public bool LootTargets = true;
        public bool RunPlugin;
        public bool UseMeditate = true;
        public bool UseInstruments = true;
        public bool LevelFamiliars;
        public bool ManualMovement;
        public bool SwitchRoutines = true;
        public bool AntiCC = true;
        public bool EscapeDeath = true;
        public bool AssistLeader;
        public bool RollOnItems;
        public bool MobTagging;
        public bool StopOnDeath;

        public string TemplateName = string.Empty;
        public string MapName = string.Empty;
        public string RunPluginName = string.Empty;
        public string FinalAction = string.Empty;
        public string FamiliarName = string.Empty;

        public int FightRadius = 60;
        public int MinHitpoints = 40;
        public int MinMana = 30;
        public int TagMobs = 1;


        [XmlArrayItem("Name")]
        public List<string> Routines { get; set; }

        [XmlArrayItem("Name")]
        public List<string> Targets { get; set; }

        [XmlArrayItem("Name")]
        public List<string> IgnoreTargets { get; set; }

        [XmlArrayItem("Name")]
        public List<string> CleanItems { get; set; }

        [XmlArrayItem("Name")]
        public List<string> ProcessItems { get; set; }

        [XmlArrayItem("Name")]
        public List<string> HpRecoverItems { get; set; }

        [XmlArrayItem("Name")]
        public List<string> ManaRecoverItems { get; set; }

        [XmlArrayItem("Name")]
        public List<string> HpPotions { get; set; }

        [XmlArrayItem("Name")]
        public List<string> CombatBoosts { get; set; }

        public Settings()
        {
            Routines = new List<string>();
            Targets = new List<string>();
            IgnoreTargets = new List<string>();
            CleanItems = new List<string>();
            ProcessItems = new List<string>();
            HpRecoverItems = new List<string>();
            ManaRecoverItems = new List<string>();
            HpPotions = new List<string>();
            CombatBoosts = new List<string>();
        }
    }
}
