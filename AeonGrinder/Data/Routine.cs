using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AeonGrinder.Data
{
    using Configs;

    public class Routine
    {
        private Host Host;
        private Dictionary<string, Template> templates;


        public string Name { get; private set; }
        public Template Template { get; private set; }

        public List<string> Rotation { get; private set; }
        public Dictionary<string, Combos> Combos { get; private set; }
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
            Rotation = Rotation.Concat(Template.Rotation).ToList();

            Combos = Template.Combos.ToDictionary(combo => combo.Name, combo => combo);
            Loader = new Queue<string>();
        }

        public string GetNext() => Rotation[Sequence];
        public string QueuePeek() => Loader.Peek();
        public string QueueTake() => Loader.Dequeue();

        public void PushNext() => Sequence = (Sequence < (Rotation.Count - 1)) ? Sequence + 1 : 0;

        public void LoadCombo(string name)
        {
            if (!Combos.ContainsKey(name))
                return;

            foreach (string combo in Combos[name].Skills) Loader.Enqueue(combo);
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
        {
            return Combos.ContainsKey(skillName);
        }


        public bool Exists()
        {
            return Exists(Name);
        }

        public bool Exists(string name)
        {
            return templates.ContainsKey(name);
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
