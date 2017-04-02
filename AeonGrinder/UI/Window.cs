using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ArcheBot.Bot.Classes;

namespace AeonGrinder.UI
{
    using Data;
    using Enums;
    using Configs;

    public partial class Window : Form
    {
        private Host Host;

        public Window(Host host)
        {
            InitializeComponent(); Host = host;
        }

        private void Window_Load(object sender, EventArgs e)
        {
            Task.Run(() => Ping());

            // Load data
            GetSkills();
            GetTargets();
            GetTemplates();
            GetZoneMaps();
            GetConditions();

            // Load saved configs
            LoadSettings();
            LoadTemplate();

            // Auto Start if enabled
            if (chkbox_AutoStart.Checked) Host.BaseModule.Start();
        }

        private void SetWindowDetails()
        {
            Text = string.Format("AeonGrinder - {0} @ {1} - Ping: {2}", Host.me.name, Host.serverName(), Host.pingToServer);
        }

        private void Ping()
        {
            while (true)
            {
                Utils.InvokeOn(this, () => SetWindowDetails());
                Utils.Sleep(1000);
            }
        }


        #region Helpers

        private void AddItemToList(Control item, ListBox inLbox, bool checkExists = false)
        {
            Utils.InvokeOn(this, () =>
            {
                object selected = null;

                if (item is ListBox)
                {
                    selected = (item as ListBox).SelectedItem;
                }
                else if (item is ComboBox)
                {
                    selected = (item as ComboBox).SelectedItem;
                }


                if (selected == null || (checkExists && inLbox.Items.Contains(selected)))
                {
                    return;
                }

                inLbox.Items.Add(selected);
            });
        }

        private void PopFromList(ListBox lbox)
        {
            Utils.InvokeOn(this, () =>
            {
                var selected = lbox.SelectedItem;

                if (selected != null) lbox.Items.Remove(selected);
            });
        }

        private bool MoveListItem(int direction, ListBox box)
        {
            bool result = false;

            Utils.InvokeOn(this, () =>
            {
                var item = box.SelectedItem;

                if (item == null || box.SelectedIndex < 0)
                    return;


                int index = box.SelectedIndex, nIndex = (index + direction);

                if (nIndex < 0 || nIndex >= box.Items.Count)
                    return;


                box.Items.RemoveAt(index);
                box.Items.Insert(nIndex, item);

                box.SetSelected(nIndex, true);
                result = true;
            });

            return result;
        }

        private void CombosMoveItem(int direction)
        {
            var s1 = lbox_ComboTriggers.SelectedItem;
            var s2 = lbox_ComboRotations.SelectedItem;

            if (s1 == null || s2 == null || !combos.ContainsKey(s1.ToString()))
                return;


            int index = lbox_ComboRotations.SelectedIndex,
                nIndex = (index + direction);

            if (nIndex < 0 || nIndex >= combos[s1.ToString()].Skills.Count)
                return;


            combos[s1.ToString()].Skills.RemoveAt(index);
            combos[s1.ToString()].Skills.Insert(nIndex, s2.ToString());

            MoveListItem(direction, lbox_ComboRotations);
        }

        public void UpdateButtonState(string text, bool state = true)
        {
            Utils.InvokeOn(btn_Begin, () =>
            {
                btn_Begin.Text = text;
                btn_Begin.Enabled = state;
            });
        }

        public void UpdateLabel(Label label, string text) => Utils.InvokeOn(this, () => label.Text = text);

        private string GetTemplateName()
        {
            string name = string.Empty;

            Utils.InvokeOn(this, () => name = cmbox_Templates.Text);


            return name;
        }

        #endregion

        #region Props & Fields

        public bool ButtonSwitch
        {
            get { return btnSwitch; } set { btnSwitch = value; }
        }

        private bool btnSwitch;
        private Task queryTask;

        private Dictionary<string, Combos> combos = new Dictionary<string, Combos>();

        #endregion

        #region Configs

        public Settings SaveSettings()
        {
            var settings = GetSettings();

            if (settings != null && Serializer.Save(settings, $"{Paths.Settings}{Host.me.name}@{Host.serverName()}.xml"))
            {
                return settings;
            }

            return null;
        }

        private void LoadSettings()
        {
            Settings settings = Serializer.Load(new Settings(), $"{Paths.Settings}{Host.me.name}@{Host.serverName()}.xml");

            if (settings == null)
                return;


            Utils.InvokeOn(this, () =>
            {
                chkbox_AutoStart.Checked = settings.AutoStart;
                chkbox_LootTargets.Checked = settings.LootTargets;
                chkbox_RunPlugin.Checked = settings.RunPlugin;

                int index = 0;

                if ((index = cmbox_Templates.Items.IndexOf(settings.TemplateName)) != -1)
                {
                    cmbox_Templates.SelectedIndex = index;
                }

                if ((index = cmbox_ZoneMaps.Items.IndexOf(settings.MapName)) != -1)
                {
                    cmbox_ZoneMaps.SelectedIndex = index;
                }

                txtbox_PluginRunName.Text = settings.RunPluginName;


                var optionA = container_WhenDone.Controls.OfType<OptionBox>().FirstOrDefault(r => r.OptionName == settings.FinalAction);

                if (optionA != null)
                {
                    optionA.Checked = true;
                }


                num_FightRadius.Value = settings.FightRadius;


                lbox_Targets.Items.AddRange(settings.Targets.ToArray());
                lbox_CleanItems.Items.AddRange(settings.CleanItems.ToArray());
            });
        }

        public Settings GetSettings()
        {
            Settings settings = new Settings();

            Utils.InvokeOn(this, () =>
            {
                settings.AutoStart = chkbox_AutoStart.Checked;
                settings.LootTargets = chkbox_LootTargets.Checked;
                settings.RunPlugin = chkbox_RunPlugin.Checked;
                settings.TemplateName = cmbox_Templates.Text;
                settings.MapName = (cmbox_ZoneMaps.SelectedIndex != 0) ? cmbox_ZoneMaps.Text : string.Empty;
                settings.RunPluginName = txtbox_PluginRunName.Text;
                settings.FightRadius = (int)num_FightRadius.Value;
                settings.FinalAction = container_WhenDone.Controls.OfType<OptionBox>().FirstOrDefault(r => r.Checked)?.OptionName;
                settings.Targets = lbox_Targets.Items.OfType<string>().ToList();
                settings.CleanItems = lbox_CleanItems.Items.OfType<string>().ToList();
            });


            return settings;
        }


        public Template SaveTemplate(string name)
        {
            var template = GetTemplate();

            if (template != null && Serializer.Save(template, $"{Paths.Templates}{name}.template"))
            {
                return template;
            }

            return null;
        }

        public bool SaveTemplate()
        {
            var name = GetTemplateName();

            return (name.Length > 1 && SaveTemplate(name) != null);
        }

        public Template GetTemplate()
        {
            Template template = new Template();

            Utils.InvokeOn(this, () =>
            {
                template.Rotation = lbox_Rotation.Items.OfType<string>().ToList();
                template.Combos = combos.Select(c => new Combos() { Name = c.Key, Skills = c.Value.Skills, Conditions = c.Value.Conditions }).ToList();
                template.BoostingBuffs = lbox_BoostingBuffs.Items.OfType<string>().ToList();
                template.CombatBuffs = lbox_CombatBuffs.Items.OfType<string>().ToList();
            });

            return template;
        }

        public void LoadTemplate(string path)
        {
            var template = Serializer.Load(new Template(), path);

            if (template == null)
                return;


            ResetTemplate();

            Utils.InvokeOn(this, () =>
            {
                lbox_Rotation.Items.AddRange(template.Rotation.ToArray());

                foreach (var combo in template.Combos)
                {
                    // Add to dictionary
                    combos.Add(combo.Name, combo);
                    // Add to combo triggers list
                    lbox_ComboTriggers.Items.Add(combo.Name);
                }

                lbox_CombatBuffs.Items.AddRange(template.CombatBuffs.ToArray());
                lbox_BoostingBuffs.Items.AddRange(template.BoostingBuffs.ToArray());
            });
        }

        public void LoadTemplate()
        {
            var name = GetTemplateName();
            var path = Paths.Templates + $"{name}.template";

            if (name.Length < 1 || !File.Exists(path))
                return;


            LoadTemplate(path);
        }

        private void ResetTemplate()
        {
            Utils.InvokeOn(this, () =>
            {
                lbox_Rotation.Items.Clear();
                lbox_ComboTriggers.Items.Clear();
                lbox_ComboRotations.Items.Clear();
                lbox_CombatBuffs.Items.Clear();
                lbox_BoostingBuffs.Items.Clear();
                
                combos.Clear();
            });
        }

        #endregion

        #region Data Manipulation

        public void GetSkills()
        {
            var classes = Host.me.getAbilities().Where(a => a.active);


            var skills = Host.me.getSkills().Where
                (s => classes.Any(c => s.db.abilityId == (int)c.id)).Select(s => s.name).OrderBy(s => s);

            if (skills.Count() < 1)
                return;


            Utils.InvokeOn(this, () =>
            {
                lbox_SkillsList.Items.Clear();
                lbox_SkillsList.Items.AddRange(skills.ToArray());
            });
        }

        private Task GetSQLItems(string match)
        {
            queryTask = Task.Run(() =>
            {
                var items = Host.sqlCore.sqlItems.Where
                    (i => i.Value.name.ToLower().Contains(match.ToLower())).Select(i => i.Value.name).Distinct()
                    .OrderBy(i => i);

                if (items.Count() == 0)
                    return;


                Utils.InvokeOn(this, () =>
                {
                    lbox_ItemsList.Items.Clear();
                    lbox_ItemsList.Items.AddRange(items.ToArray());
                });
            });

            return queryTask;
        }

        private void GetInventoryItems(string match = "")
        {
            var items = Host.getAllInvItems().Select(i => i.name).Where
                (i => i != string.Empty).Distinct();

            if (items.Count() == 0)
                return;


            if (match != string.Empty)
            {
                items = items.Where(i => i.ToLower().Contains(match.ToLower()));
            }

            items = items.OrderBy(i => i);


            Utils.InvokeOn(this, () =>
            {
                lbox_ItemsList.Items.Clear();
                lbox_ItemsList.Items.AddRange(items.ToArray());
            });
        }

        private void GetTemplates()
        {
            var files = Directory.GetFiles(Paths.Templates, "*.template").Select(f => Path.GetFileNameWithoutExtension(f));

            if (files.Count() < 1)
                return;


            Utils.InvokeOn(this, () =>
            {
                cmbox_Templates.Items.Clear();
                cmbox_Templates.Items.AddRange(files.ToArray());
                cmbox_Templates.SelectedIndex = 0;
            });
        }

        public void GetTargets()
        {
            var targets = Host.getCreatures().Where
                (c => c.type == BotTypes.Npc && c.isAlive() && Host.isAttackable(c)).Select(c => c.name).Distinct();

            if (targets.Count() < 1)
                return;


            Utils.InvokeOn(this, () =>
            {
                cmbox_Targets.Items.Clear();
                cmbox_Targets.Items.AddRange(targets.ToArray());
                cmbox_Targets.SelectedIndex = 0;
            });
        }

        private void GetZoneMaps()
        {
            var maps = MapsHelper.GetAll().Select(m => m.Name);

            Utils.InvokeOn(this, () =>
            {
                cmbox_ZoneMaps.Items.Clear();
                cmbox_ZoneMaps.Items.Add("No map selected");

                if (maps.Count() > 0)
                {
                    cmbox_ZoneMaps.Items.AddRange(maps.ToArray());
                }
                
                cmbox_ZoneMaps.SelectedIndex = 0;
            });
        }

        private void GetConditions()
        {
            Utils.InvokeOn(this, () =>
            {
                foreach (var type in Enum.GetValues(typeof(ConditionType)))
                {
                    cmbox_Conditions.Items.Add(new ComboBoxItem() { Text = type.ToString(), Value = type });
                }

                cmbox_Conditions.SelectedIndex = 0;
            });
        }

        #endregion

        #region Click Events

        private void btn_Begin_Click(object sender, EventArgs e)
        {
            if (!ButtonSwitch)
            {
                Host.BaseModule.Start();
            }
            else
            {
                Host.BaseModule.Stop();
            }
        }

        private void btn_SaveTemplate_Click(object sender, EventArgs e)
        {
            var name = GetTemplateName();

            if (name.Length < 1 || SaveTemplate(name) == null)
                return;


            GetTemplates();

            Utils.InvokeOn(this, () =>
            {
                int index = cmbox_Templates.Items.IndexOf(name);

                if (index != -1)
                {
                    cmbox_Templates.SelectedIndex = index;
                }
            });
        }

        private void btn_LoadTemplate_Click(object sender, EventArgs e) => LoadTemplate();
        private void btn_GetInventoryItems_Click(object sender, EventArgs e) => GetInventoryItems();

        private void btn_AddToRotation_Click(object sender, EventArgs e) => AddItemToList(lbox_SkillsList, lbox_Rotation);
        private void lbox_Rotation_DoubleClick(object sender, EventArgs e) => PopFromList(lbox_Rotation);
        private void btn_MoveRotationUp_Click(object sender, EventArgs e) => MoveListItem(-1, lbox_Rotation);
        private void btn_MoveRotationDown_Click(object sender, EventArgs e) => MoveListItem(1, lbox_Rotation);
        
        private void btn_AddToCombatBuffs_Click(object sender, EventArgs e) => AddItemToList(lbox_SkillsList, lbox_CombatBuffs, true);
        private void lbox_CombatBuffs_DoubleClick(object sender, EventArgs e) => PopFromList(lbox_CombatBuffs);
        private void btn_MoveCombatBuffsUp_Click(object sender, EventArgs e) => MoveListItem(-1, lbox_CombatBuffs);
        private void btn_MoveCombatBuffsDown_Click(object sender, EventArgs e) => MoveListItem(1, lbox_CombatBuffs);

        private void btn_AddToBoostingBuffs_Click(object sender, EventArgs e) => AddItemToList(lbox_SkillsList, lbox_BoostingBuffs, true);
        private void lbox_BoostingBuffs_DoubleClick(object sender, EventArgs e) => PopFromList(lbox_BoostingBuffs);
        private void btn_MoveBoostingBuffsUp_Click(object sender, EventArgs e) => MoveListItem(-1, lbox_BoostingBuffs);
        private void btn_MoveBoostingBuffsDown_Click(object sender, EventArgs e) => MoveListItem(1, lbox_BoostingBuffs);
        
        private void btn_AddToCleanItems_Click(object sender, EventArgs e) => AddItemToList(lbox_ItemsList, lbox_CleanItems, true);
        private void lbox_CleanItems_DoubleClick(object sender, EventArgs e) => PopFromList(lbox_CleanItems);

        private void btn_AddToTargets_Click(object sender, EventArgs e) => AddItemToList(cmbox_Targets, lbox_Targets, true);
        private void lbox_Targets_DoubleClick(object sender, EventArgs e) => PopFromList(lbox_Targets);


        private void btn_AddToComboTriggers_Click(object sender, EventArgs e)
        {
            Utils.InvokeOn(this, () =>
            {
                var selected = lbox_Rotation.SelectedItem;

                if (selected == null)
                    return;


                if (!combos.ContainsKey(selected.ToString()))
                {
                    combos.Add(selected.ToString(), new Combos());
                }

                if (!lbox_ComboTriggers.Items.Contains(selected)) lbox_ComboTriggers.Items.Add(selected);
            });
        }

        private void lbox_ComboTriggers_DoubleClick(object sender, EventArgs e)
        {
            Utils.InvokeOn(this, () =>
            {
                var selected = lbox_ComboTriggers.SelectedItem;

                if (selected == null)
                    return;


                if (combos.ContainsKey(selected.ToString()))
                {
                    combos.Remove(selected.ToString());
                }

                if (lbox_ComboTriggers.Items.Contains(selected)) lbox_ComboTriggers.Items.Remove(selected);
            });
        }

        private void lbox_ComboTriggers_SelectedIndexChanged(object sender, EventArgs e)
        {
            Utils.InvokeOn(this, () =>
            {
                var selected = lbox_ComboTriggers.SelectedItem;

                if (selected == null)
                    return;

                if (!combos.ContainsKey(selected.ToString()) && lbox_ComboTriggers.Items.Contains(selected))
                {
                    lbox_ComboTriggers.Items.Remove(selected);

                    return;
                }


                var combo = combos[selected.ToString()];

                lbox_ComboRotations.Items.Clear();
                lbox_ComboRotations.Items.AddRange(combo.Skills.ToArray());

                lbox_Conditions.Items.Clear();
                lbox_Conditions.Items.AddRange(combo.Conditions.Select(c => c.Name).ToArray());
            });
        }


        private void btn_AddToComboRotations_Click(object sender, EventArgs e)
        {
            Utils.InvokeOn(this, () =>
            {
                var s1 = lbox_SkillsList.SelectedItem;
                var s2 = lbox_ComboTriggers.SelectedItem;

                if (s1 == null || s2 == null || !combos.ContainsKey(s2.ToString()))
                    return;


                combos[s2.ToString()].Skills.Add(s1.ToString());
                lbox_ComboRotations.Items.Add(s1);
            });
        }

        private void lbox_ComboRotations_DoubleClick(object sender, EventArgs e)
        {
            Utils.InvokeOn(this, () =>
            {
                var s1 = lbox_ComboTriggers.SelectedItem;
                var s2 = lbox_ComboRotations.SelectedItem;

                if (s1 == null || s2 == null || !combos.ContainsKey(s1.ToString()))
                    return;


                int index = lbox_ComboRotations.Items.IndexOf(s2);
                lbox_ComboRotations.Items.Remove(s2);

                if (index != -1)
                {
                    combos[s1.ToString()].Skills.RemoveAt(index);
                }
            });
        }

        private void btn_MoveComboRotationUp_Click(object sender, EventArgs e) => CombosMoveItem(-1);
        private void btn_MoveComboRotationDown_Click(object sender, EventArgs e) => CombosMoveItem(1);


        private void btn_AddCondition_Click(object sender, EventArgs e)
        {
            Utils.InvokeOn(this, () =>
            {
                var s1 = lbox_ComboTriggers.SelectedItem;
                var s2 = cmbox_Conditions.SelectedItem as ComboBoxItem;

                if (s1 == null || s2 == null || !combos.ContainsKey(s1.ToString()))
                    return;

                var combo = combos[s1.ToString()];

                
                var condType = (ConditionType)s2.Value;

                if (combo.Conditions.Any(c => c.Type == condType))
                    return;
                

                var condValue = txtbox_ConditionValue.Text;

                if (condValue.Length < 1)
                    return;
                

                var condName = $"{s2.Text} | {condValue}";

                combo.Conditions.Add(new Condition() { Name = condName, Type = condType, Value = condValue });
                lbox_Conditions.Items.Add(condName);
            });
        }

        private void lbox_Conditions_DoubleClick(object sender, EventArgs e)
        {
            Utils.InvokeOn(this, () =>
            {
                var s1 = lbox_ComboTriggers.SelectedItem;
                var s2 = lbox_Conditions.SelectedItem;

                if (s1 == null || s2 == null || !combos.ContainsKey(s1.ToString()))
                    return;


                int index = lbox_Conditions.Items.IndexOf(s2);
                lbox_Conditions.Items.Remove(s2);

                if (index != -1)
                {
                    combos[s1.ToString()].Conditions.RemoveAt(index);
                }
            });
        }

        #endregion

        #region Other Events

        private async void txtbox_ItemSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter
                || queryTask != null && queryTask.Status == TaskStatus.Running)
                return;

            // Suppress defaults
            e.SuppressKeyPress = true;


            string match = string.Empty;

            Utils.InvokeOn(this, () =>
            {
                match = txtbox_ItemSearch.Text;

                if (match.Length < 3)
                    return;


                txtbox_ItemSearch.Enabled = false;
            });

            if (match.Length < 3)
                return;


            await GetSQLItems(match);

            Utils.InvokeOn(this, () => txtbox_ItemSearch.Enabled = true);
        }

        #endregion
    }
}
