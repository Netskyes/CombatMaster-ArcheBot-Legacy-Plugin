using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CombatMaster.Data
{
    using Enums;
    using Configs;

    public class Routine
    {
        private Host Host;

        // Available templates
        private Dictionary<string, Template> templates;

        // Resources
        public string Name { get; private set; }
        public Template Template { get; private set; }

        // Routine Core
        public List<string> Rotation { get; private set; }
        public Dictionary<List<string>, List<string>> Combos { get; private set; }
        public Queue<string> Loader { get; private set; }
        public int Sequence { get; private set; }


        public Routine(Host host, List<Template> templates)
        {
            Host = host;
            this.templates = templates.ToDictionary(t => t.Name, t => t);
        }

        private Template GetTemplate(string name)
        {
            return Exists(name) ? templates[name] : null;
        }

        
        public void Build(string name)
        {
            Template = GetTemplate(name);

            if (Template == null)
                return;


            Name = Template.Name;

            // Build routine
            Rotation = Template.CombatBuffs;

            var random = new Random();
            Rotation = Rotation.OrderBy(b => random.Next()).ToList();
            Rotation = Rotation.Concat(Template.Rotation).ToList();

            Combos = Template.Combos.ToDictionary(c => c.Triggers, c => c.Skills);
            Loader = new Queue<string>();
        }

        public string GetNext() => Rotation[Sequence];
        public string QueuePeek() => Loader.Peek();
        public string QueueTake() => Loader.Dequeue();

        public void PushNext() => Sequence = (Sequence < (Rotation.Count - 1)) ? Sequence + 1 : 0;

        public void LoadCombo(string name)
        {
            var combos = GetCombo(name);
            if (combos == null)
                return;
            
            foreach (string combo in combos) Loader.Enqueue(combo);
        }

        public void EmptyLoader()
        {
            Sequence = 0;
            Loader.Clear();
        }


        public bool IsCombatBuff(string skillName)
        {
            return Template.CombatBuffs.Contains(skillName);
        }

        public bool IsBoostingBuff(string skillName)
        {
            return Template.BoostingBuffs.Contains(skillName);
        }

        public bool IsCombo(string skillName) 
            => (GetCombo(skillName) != null);

        public bool IsLoaded(string skillName) 
            => (Loader.Contains(skillName));

        public List<string> GetCombo(string skillName)
        {
            var combo = Combos.Where
                (c => c.Key.Any(s => s == skillName));

            return (combo.Count() > 0) ? combo.FirstOrDefault().Value : null;
        }

        public List<Condition> GetConditions(string skillName)
        {
            return Template.CastConditions.Find(c => c.SkillName == skillName)?.ConditionsList ?? null;
        }

        public List<string> GetHeals() => Template.Heals;
        public List<string> GetCombatBuffs() => Template.CombatBuffs;
        public List<string> GetBoostingBuffs() => Template.BoostingBuffs;
        


        public bool Exists()
        {
            return Exists(Name);
        }

        public bool Exists(string name)
        {
            return name != null && templates.ContainsKey(name);
        }

        public bool IsValid()
        {
            return IsValid(Name);
        }

        public bool IsValid(string name)
        {
            return Exists(name) && templates[name].Rotation.Count > 0;
        }
    }
}
