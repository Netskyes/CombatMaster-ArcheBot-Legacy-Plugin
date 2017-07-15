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

namespace CombatMaster.UI
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
            GetSlaves();
            GetTemplatesList();
            GetZoneMaps();
            SetConditions();
            SetTooltips();

            // Load saved configs
            LoadSettings();
            LoadTemplate();

            // Auto Start if enabled
            if (chkbox_AutoStart.Checked) Host.BaseModule.Start();

            // Events
            Host.onKeyDown += OnKeyDown;
        }

        private void SetWindowDetails()
        {
            Text = string.Format("Combat AIO - {0} @ {1} - Ping: {2}", Host.me.name, Host.serverName(), Host.pingToServer);
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

        private void BaseBegin()
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
                int index = lbox.SelectedIndex;

                if (index != -1) lbox.Items.RemoveAt(index);
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
            var s1 = lbox_Combos.SelectedItem;
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

        private string[] GetTemplateNames()
        {
            string[] templates = new string[] {};

            Utils.InvokeOn(this, () => { templates = cmbox_Templates.Items.OfType<string>().ToArray(); });


            return templates;
        }

        public bool StatsReset()
        {
            bool result = false;

            Utils.InvokeOn(this, () =>
            {
                result = chkbox_ResetStats.Checked;

                if (result)
                {
                    chkbox_ResetStats.Checked = false;
                }
            });


            return result;
        }

        public void ClearStatsBags() => Utils.InvokeOn(this, () => { dtg_Items.Rows.Clear(); dtg_Mobs.Rows.Clear(); });

        #endregion

        #region Props & Fields

        public bool ButtonSwitch
        {
            get { return btnSwitch; } set { btnSwitch = value; }
        }

        private bool btnSwitch;
        private Task queryTask;

        private Dictionary<string, Combos> combos = new Dictionary<string, Combos>();
        private Dictionary<string, List<Condition>> conditions = new Dictionary<string, List<Condition>>();

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

        private void LoadSettings(Settings proxy = null)
        {
            var settings = (proxy != null) 
                ? proxy : Serializer.Load(new Settings(), $"{Paths.Settings}{Host.me.name}@{Host.serverName()}.xml");

            if (settings == null)
            {
                settings = new Settings();
            }


            Utils.InvokeOn(this, () =>
            {
                chkbox_AutoStart.Checked = settings.AutoStart;
                chkbox_LootTargets.Checked = settings.LootTargets;
                chkbox_RunPlugin.Checked = settings.RunPlugin;
                chkbox_UseMeditate.Checked = settings.UseMeditate;
                chkbox_UseInstruments.Checked = settings.UseInstruments;
                chkbox_LevelFamiliars.Checked = settings.LevelFamiliars;
                chkbox_ManualMovement.Checked = settings.ManualMovement;
                chkbox_SwitchRoutines.Checked = settings.SwitchRoutines;
                chkbox_AntiCC.Checked = settings.AntiCC;
                chkbox_EscapeDeath.Checked = settings.EscapeDeath;
                chkbox_AssistLeader.Checked = settings.AssistLeader;
                chkbox_RollOnItems.Checked = settings.RollOnItems;


                int index = 0;

                if ((index = cmbox_Templates.Items.IndexOf(settings.TemplateName)) != -1)
                {
                    cmbox_Templates.SelectedIndex = index;
                }

                if ((index = cmbox_ZoneMaps.Items.IndexOf(settings.MapName)) != -1)
                {
                    cmbox_ZoneMaps.SelectedIndex = index;
                }

                if ((index = cmbox_Familiars.Items.IndexOf(settings.FamiliarName)) != -1)
                {
                    cmbox_Familiars.SelectedIndex = index;
                }

                txtbox_PluginRunName.Text = settings.RunPluginName;


                var optionA = container_WhenDone.Controls.OfType<OptionBox>().FirstOrDefault(r => r.OptionName == settings.FinalAction);

                if (optionA != null)
                {
                    optionA.Checked = true;
                }


                num_FightRadius.Value = settings.FightRadius;
                num_MinHitpoints.Value = settings.MinHitpoints;
                num_MinMana.Value = settings.MinMana;


                lbox_Routines.Items.AddRange(settings.Routines.ToArray());
                lbox_Targets.Items.AddRange(settings.Targets.ToArray());
                lbox_IgnoreTargets.Items.AddRange(settings.IgnoreTargets.ToArray());
                lbox_CleanItems.Items.AddRange(settings.CleanItems.ToArray());
                lbox_ProcessItems.Items.AddRange(settings.ProcessItems.ToArray());
                lbox_HpRecoverItems.Items.AddRange(settings.HpRecoverItems.ToArray());
                lbox_ManaRecoverItems.Items.AddRange(settings.ManaRecoverItems.ToArray());
                lbox_HpPotions.Items.AddRange(settings.HpPotions.ToArray());
                lbox_CombatBoosts.Items.AddRange(settings.CombatBoosts.ToArray());
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
                settings.UseMeditate = chkbox_UseMeditate.Checked;
                settings.UseInstruments = chkbox_UseInstruments.Checked;
                settings.LevelFamiliars = chkbox_LevelFamiliars.Checked;
                settings.ManualMovement = chkbox_ManualMovement.Checked;
                settings.SwitchRoutines = chkbox_SwitchRoutines.Checked;
                settings.AntiCC = chkbox_AntiCC.Checked;
                settings.EscapeDeath = chkbox_EscapeDeath.Checked;
                settings.AssistLeader = chkbox_AssistLeader.Checked;
                settings.RollOnItems = chkbox_RollOnItems.Checked;
                settings.TemplateName = GetTemplateName();
                settings.MapName = (cmbox_ZoneMaps.SelectedIndex != 0) ? cmbox_ZoneMaps.Text : string.Empty;
                settings.RunPluginName = txtbox_PluginRunName.Text;
                settings.FamiliarName = cmbox_Familiars.Text;
                settings.FightRadius = (int)num_FightRadius.Value;
                settings.MinHitpoints = (int)num_MinHitpoints.Value;
                settings.MinMana = (int)num_MinMana.Value;
                settings.FinalAction = container_WhenDone.Controls.OfType<OptionBox>().FirstOrDefault(r => r.Checked)?.OptionName;
                settings.Routines = lbox_Routines.Items.OfType<string>().ToList();
                settings.Targets = lbox_Targets.Items.OfType<string>().ToList();
                settings.IgnoreTargets = lbox_IgnoreTargets.Items.OfType<string>().ToList();
                settings.CleanItems = lbox_CleanItems.Items.OfType<string>().ToList();
                settings.ProcessItems = lbox_ProcessItems.Items.OfType<string>().ToList();
                settings.HpRecoverItems = lbox_HpRecoverItems.Items.OfType<string>().ToList();
                settings.ManaRecoverItems = lbox_ManaRecoverItems.Items.OfType<string>().ToList();
                settings.HpPotions = lbox_HpPotions.Items.OfType<string>().ToList();
                settings.CombatBoosts = lbox_CombatBoosts.Items.OfType<string>().ToList();
            });


            return settings;
        }


        public Template SaveTemplate(string name)
        {
            var template = FetchTemplate();

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

        public Template GetTemplate(string path) => Serializer.Load(new Template(), path);

        public Template FetchTemplate()
        {
            Template template = new Template();

            Utils.InvokeOn(this, () =>
            {
                template.Name = GetTemplateName();
                template.MinHpHeals = (int)num_MinHpHeals.Value;
                template.Rotation = lbox_Rotation.Items.OfType<string>().ToList();
                template.Combos = combos.Select(c => new Combos(c.Key, c.Value.Skills, c.Value.Triggers)).ToList();
                template.Heals = lbox_Heals.Items.OfType<string>().ToList();
                template.CastConditions = conditions.Select(c => new Conditions() { SkillName = c.Key, ConditionsList = c.Value }).ToList();
                template.BoostingBuffs = lbox_BoostingBuffs.Items.OfType<string>().ToList();
                template.CombatBuffs = lbox_CombatBuffs.Items.OfType<string>().ToList();
            });

            return template;
        }

        public List<Template> GetTemplates()
        {
            return GetTemplateNames().Select(name => GetTemplate(Paths.Templates + $"{name}.template")).Where(t => t != null && t.Name != null).ToList();
        }

        public void LoadTemplate(string path)
        {
            var template = Serializer.Load(new Template(), path);

            if (template == null)
                return;


            ResetTemplate();

            Utils.InvokeOn(this, () =>
            {
                num_MinHpHeals.Value = template.MinHpHeals;


                lbox_Rotation.Items.AddRange(template.Rotation.ToArray());

                foreach (var combo in template.Combos.Where(c => c.Name != null))
                {
                    // Add to dictionary
                    combos.Add(combo.Name, combo);
                    // Add to combo triggers list
                    lbox_Combos.Items.Add(combo.Name);
                }

                if (lbox_Combos.Items.Count > 0)
                {
                    lbox_Combos.SelectedIndex = 0;
                }
                
                foreach (var cond in template.CastConditions.Where(c => c.SkillName != null))
                {
                    conditions.Add(cond.SkillName, cond.ConditionsList);
                }
                
                lbox_CombatBuffs.Items.AddRange(template.CombatBuffs.ToArray());
                lbox_BoostingBuffs.Items.AddRange(template.BoostingBuffs.ToArray());
                lbox_Heals.Items.AddRange(template.Heals.ToArray());
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
                num_MinHpHeals.Value = 35;
                
                lbox_Rotation.Items.Clear();
                lbox_Combos.Items.Clear();
                lbox_ComboRotations.Items.Clear();
                lbox_CombatBuffs.Items.Clear();
                lbox_BoostingBuffs.Items.Clear();
                lbox_Heals.Items.Clear();
                
                combos.Clear();
                conditions.Clear();
            });
        }

        #endregion

        #region Data Manipulation

        public void GetSkills()
        {
            var classes = Host.me.getAbilities().Where(a => a.active);
            uint[] ignores = { Abilities.Witchcraft.PlayDead, Abilities.Auramancy.Meditate };
            byte[] weapons = { 15, 16 };


            var skills = Host.me.getSkills().Where
                (s => (classes.Any(c => s.db.abilityId == (int)c.id) && !ignores.Contains(s.id)))
                .Select(s => s.name).OrderBy(s => s);

            var itemUses = Host.me.getAllEquipedItems().Where
                (i => weapons.Contains(i.cell) && i.db.useSkillId != 0);

            if (skills.Count() < 1)
                return;


            Utils.InvokeOn(this, () =>
            {
                lbox_SkillsList.Items.Clear();
                lbox_SkillsList.Items.AddRange(skills.ToArray());
                
                if (itemUses.Count() > 0)
                {
                    lbox_SkillsList.Items.AddRange(itemUses.Select(i => i.name).ToArray());
                }
            });
        }

        public void GetSlaves()
        {
            var slaves = Host.getAllInvItems().Where
                (i => Slaves.ItemExists(i.id)).OrderBy(i => i.name).Select(i => i.name);

            if (slaves.Count() < 1)
                return;


            Utils.InvokeOn(this, () =>
            {
                cmbox_Familiars.Items.Clear();
                cmbox_Familiars.Items.AddRange(slaves.ToArray());
                cmbox_Familiars.SelectedIndex = 0;
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

        private void GetTemplatesList()
        {
            var files = Directory.GetFiles(Paths.Templates, "*.template").Select(f => Path.GetFileNameWithoutExtension(f));

            if (files.Count() < 1)
                return;


            Utils.InvokeOn(this, () =>
            {
                cmbox_Templates.Items.Clear();
                cmbox_Templates.Items.AddRange(files.ToArray());
                cmbox_Templates.SelectedIndex = 0;

                cmbox_Routines.Items.Clear();
                cmbox_Routines.Items.AddRange(files.ToArray());
                cmbox_Routines.SelectedIndex = 0;
            });
        }

        public void GetTargets()
        {
            var targets = Host.getCreatures().Where
                (c => c.type == BotTypes.Npc && c.isAlive() && (Host.isAttackable(c) || Host.isEnemy(c)))
                .Select(c => c.name).Distinct();

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

        private void SetConditions()
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

        private void SetTooltips()
        {
            var tooltip = new ToolTip();
        }

        private void AddToGridBag(object tag, string name, int count, DataGridView dtg)
        {
            Utils.InvokeOn(this, () =>
            {
                var row = dtg.Rows.OfType<DataGridViewRow>().FirstOrDefault(d => d.Tag == tag);

                if (row != null)
                {
                    int amount = 0;

                    try
                    {
                        amount = Convert.ToInt32(row.Cells[1].Value);
                    }
                    catch
                    {
                    }

                    row.Cells[1].Value = (amount + count);

                    return;
                }


                int index = dtg.Rows.Add(name, count);

                try
                {
                    dtg.Rows[index].Tag = tag;
                }
                catch
                {
                }
            });
        }

        public void AddToGridBag(Item item, int count) => AddToGridBag(item.name, item.name, count, dtg_Items);
        public void AddToGridBag(Creature obj) => AddToGridBag(obj.name, obj.name, 1, dtg_Mobs);

        #endregion

        #region Click Events

        private void btn_Begin_Click(object sender, EventArgs e) => BaseBegin();

        private void btn_SaveTemplate_Click(object sender, EventArgs e)
        {
            var name = GetTemplateName();

            if (name.Length < 1 || SaveTemplate(name) == null)
                return;


            GetTemplatesList();

            Utils.InvokeOn(this, () =>
            {
                int index = cmbox_Templates.Items.IndexOf(name);

                if (index != -1)
                {
                    cmbox_Templates.SelectedIndex = index;
                }
            });
        }

        private void btn_AddToRoutines_Click(object sender, EventArgs e) => AddItemToList(cmbox_Routines, lbox_Routines, true);
        private void btn_MoveRoutinesUp_Click(object sender, EventArgs e) => MoveListItem(-1, lbox_Routines);
        private void btn_MoveRoutinesDown_Click(object sender, EventArgs e) => MoveListItem(1, lbox_Routines);

        private void btn_LoadTemplate_Click(object sender, EventArgs e) => LoadTemplate();
        private void btn_GetInventoryItems_Click(object sender, EventArgs e) => GetInventoryItems();
        private void ClearTemplate_Click(object sender, EventArgs e) => ResetTemplate();

        private void btn_AddToHpRecover_Click(object sender, EventArgs e) => AddItemToList(lbox_ItemsList, lbox_HpRecoverItems, true);
        private void btn_AddToManaRecover_Click(object sender, EventArgs e) => AddItemToList(lbox_ItemsList, lbox_ManaRecoverItems, true);
        private void btn_AddToHpPotions_Click(object sender, EventArgs e) => AddItemToList(lbox_ItemsList, lbox_HpPotions, true);
        private void btn_AddToCombatBoosts_Click(object sender, EventArgs e) => AddItemToList(lbox_ItemsList, lbox_CombatBoosts, true);

        private void btn_AddToCleanItems_Click(object sender, EventArgs e) => AddItemToList(lbox_ItemsList, lbox_CleanItems, true);
        private void btn_AddToProcessItems_Click(object sender, EventArgs e) => AddItemToList(lbox_ItemsList, lbox_ProcessItems, true);
        private void btn_AddToTargets_Click(object sender, EventArgs e) => AddItemToList(cmbox_Targets, lbox_Targets, true);

        private void btn_AddToIgnoreTargets_Click(object sender, EventArgs e) => AddItemToList(cmbox_Targets, lbox_IgnoreTargets, true);
        private void btn_MoveTargetsUp_Click(object sender, EventArgs e) => MoveListItem(-1, lbox_Targets);
        private void btn_MoveTargetsDown_Click(object sender, EventArgs e) => MoveListItem(1, lbox_Targets);

        private void btn_AddToRotation_Click(object sender, EventArgs e) => AddItemToList(lbox_SkillsList, lbox_Rotation);
        private void btn_MoveRotationUp_Click(object sender, EventArgs e) => MoveListItem(-1, lbox_Rotation);
        private void btn_MoveRotationDown_Click(object sender, EventArgs e) => MoveListItem(1, lbox_Rotation);

        private void btn_AddToHeals_Click(object sender, EventArgs e) => AddItemToList(lbox_SkillsList, lbox_Heals, true);
        private void btn_MoveHealsUp_Click(object sender, EventArgs e) => MoveListItem(-1, lbox_Heals);
        private void btn_MoveHealsDown_Click(object sender, EventArgs e) => MoveListItem(1, lbox_Heals);

        private void btn_AddToCombatBuffs_Click(object sender, EventArgs e) => AddItemToList(lbox_SkillsList, lbox_CombatBuffs, true);
        private void btn_MoveCombatBuffsUp_Click(object sender, EventArgs e) => MoveListItem(-1, lbox_CombatBuffs);
        private void btn_MoveCombatBuffsDown_Click(object sender, EventArgs e) => MoveListItem(1, lbox_CombatBuffs);

        private void btn_AddToBoostingBuffs_Click(object sender, EventArgs e) => AddItemToList(lbox_SkillsList, lbox_BoostingBuffs, true);
        private void btn_MoveBoostingBuffsUp_Click(object sender, EventArgs e) => MoveListItem(-1, lbox_BoostingBuffs);
        private void btn_MoveBoostingBuffsDown_Click(object sender, EventArgs e) => MoveListItem(1, lbox_BoostingBuffs);


        private void btn_AddToCombos_Click(object sender, EventArgs e)
        {
            Utils.InvokeOn(this, () =>
            {
                var name = txtbox_ComboName.Text;

                if (name.Length < 1)
                    return;


                if (!combos.ContainsKey(name))
                {
                    combos.Add(name, new Combos());
                }

                if (!lbox_Combos.Items.Contains(name)) lbox_Combos.Items.Add(name);
            });
        }

        private void btn_AddToComboTriggers_Click(object sender, EventArgs e)
        {
            Utils.InvokeOn(this, () =>
            {
                var s1 = lbox_SkillsList.SelectedItem;
                var s2 = lbox_Combos.SelectedItem;

                if (s1 == null || s2 == null || !combos.ContainsKey(s2.ToString()))
                    return;


                combos[s2.ToString()].Triggers.Add(s1.ToString());
                lbox_ComboTriggers.Items.Add(s1);
            });
        }

        private void btn_AddToComboRotations_Click(object sender, EventArgs e)
        {
            Utils.InvokeOn(this, () =>
            {
                var s1 = lbox_SkillsList.SelectedItem;
                var s2 = lbox_Combos.SelectedItem;

                if (s1 == null || s2 == null || !combos.ContainsKey(s2.ToString()))
                    return;


                combos[s2.ToString()].Skills.Add(s1.ToString());
                lbox_ComboRotations.Items.Add(s1);
            });
        }

        private void btn_MoveComboRotationUp_Click(object sender, EventArgs e) => CombosMoveItem(-1);
        private void btn_MoveComboRotationDown_Click(object sender, EventArgs e) => CombosMoveItem(1);


        private void btn_AddCondition_Click(object sender, EventArgs e)
        {
            Utils.InvokeOn(this, () =>
            {
                var s1 = lbox_SkillsList.SelectedItem;
                var s2 = cmbox_Conditions.SelectedItem as ComboBoxItem;

                if (s1 == null || s2 == null)
                    return;


                var keyName = s1.ToString();

                if (!conditions.ContainsKey(keyName))
                {
                    conditions.Add(keyName, new List<Condition>());
                }


                var type = (ConditionType)s2.Value;
                var value = txtbox_ConditionValue.Text;

                if (value.Length < 1)
                    return;

                if (conditions[keyName].Any(c => c.Type == type))
                    return;


                var name = $"{s2.Text} | {value}";

                conditions[keyName].Add(new Condition() { Name = name, Type = type, Value = value });
                lbox_Conditions.Items.Add(name);
            });
        }

        private void btn_TemplateOptions_Click(object sender, EventArgs e)
        {
            var context = new ContextMenuStrip();
            context.ShowImageMargin = false;

            var item1 = context.Items.Add("Clear Fields");
            var item2 = context.Items.Add("Delete Template");
            var item3 = context.Items.Add("Help");

            item1.Click += ClearTemplate_Click;
            item2.Click += DeleteTemplate_Click;
            item3.Click += TemplateHelp_Click;


            context.Show(btn_TemplateOptions, new Point(0, btn_TemplateOptions.Height - 1));
        }

        private void TemplateHelp_Click(object sender, EventArgs e)
        {
            var imgview = new ImageView();

            imgview.SetImage(Properties.Resources.help);
            imgview.ShowDialog();
        }

        private void DeleteTemplate_Click(object sender, EventArgs e)
        {
            var prompt = MessageBox.Show("Are you sure?", "Delete Template", MessageBoxButtons.YesNo);

            if (prompt != DialogResult.Yes)
                return;


            var path = Paths.Templates + GetTemplateName() + ".template";

            if (!File.Exists(path))
                return;


            try
            {
                File.Delete(path);
            }
            catch
            {
                return;
            }

            GetTemplatesList();
        }

        #endregion

        #region Double Click Events

        private void lbox_Rotation_DoubleClick(object sender, EventArgs e) => PopFromList(lbox_Rotation);
        private void lbox_CombatBuffs_DoubleClick(object sender, EventArgs e) => PopFromList(lbox_CombatBuffs);
        private void lbox_Heals_DoubleClick(object sender, EventArgs e) => PopFromList(lbox_Heals);
        private void lbox_BoostingBuffs_DoubleClick(object sender, EventArgs e) => PopFromList(lbox_BoostingBuffs);
        private void lbox_Routines_DoubleClick(object sender, EventArgs e) => PopFromList(lbox_Routines);
        private void lbox_Targets_DoubleClick(object sender, EventArgs e) => PopFromList(lbox_Targets);
        private void lbox_IgnoreTargets_DoubleClick(object sender, EventArgs e) => PopFromList(lbox_IgnoreTargets);
        private void lbox_CleanItems_DoubleClick(object sender, EventArgs e) => PopFromList(lbox_CleanItems);
        private void lbox_ProcessItems_DoubleClick(object sender, EventArgs e) => PopFromList(lbox_ProcessItems);
        private void lbox_HpRecoverItems_DoubleClick(object sender, EventArgs e) => PopFromList(lbox_HpRecoverItems);
        private void lbox_ManaRecoverItems_DoubleClick(object sender, EventArgs e) => PopFromList(lbox_ManaRecoverItems);
        private void lbox_HpPotions_DoubleClick(object sender, EventArgs e) => PopFromList(lbox_HpPotions);
        private void lbox_CombatBoosts_DoubleClick(object sender, EventArgs e) => PopFromList(lbox_CombatBoosts);


        private void lbox_Combos_DoubleClick(object sender, EventArgs e)
        {
            Utils.InvokeOn(this, () =>
            {
                var selected = lbox_Combos.SelectedItem;

                if (selected == null)
                    return;


                if (combos.ContainsKey(selected.ToString()))
                {
                    combos.Remove(selected.ToString());
                }

                if (lbox_Combos.Items.Contains(selected)) lbox_Combos.Items.Remove(selected);
            });
        }

        private void lbox_ComboTriggers_DoubleClick(object sender, EventArgs e)
        {
            Utils.InvokeOn(this, () =>
            {
                var s1 = lbox_Combos.SelectedItem;
                var s2 = lbox_ComboTriggers.SelectedItem;

                if (s1 == null || s2 == null || !combos.ContainsKey(s1.ToString()))
                    return;


                lbox_ComboTriggers.Items.Remove(s2);
                combos[s1.ToString()].Triggers.Remove(s2.ToString());
            });
        }

        private void lbox_ComboRotations_DoubleClick(object sender, EventArgs e)
        {
            Utils.InvokeOn(this, () =>
            {
                var s1 = lbox_Combos.SelectedItem;
                var s2 = lbox_ComboRotations.SelectedIndex;

                if (s1 == null || s2 == -1 || !combos.ContainsKey(s1.ToString()))
                    return;


                lbox_ComboRotations.Items.RemoveAt(s2);
                combos[s1.ToString()].Skills.RemoveAt(s2);
            });
        }

        private void lbox_Conditions_DoubleClick(object sender, EventArgs e)
        {
            Utils.InvokeOn(this, () =>
            {
                var s1 = lbox_SkillsList.SelectedItem;
                var s2 = lbox_Conditions.SelectedItem;

                if (s1 == null || s2 == null || !conditions.ContainsKey(s1.ToString()))
                    return;

                var keyName = s1.ToString();
                

                int index = lbox_Conditions.Items.IndexOf(s2);
                lbox_Conditions.Items.Remove(s2);

                if (index != -1)
                {
                    conditions[keyName].RemoveAt(index);

                    if (conditions[keyName].Count < 1) conditions.Remove(keyName);
                }
            });
        }

        #endregion

        #region Index Changed Events

        private void lbox_Combos_SelectedIndexChanged(object sender, EventArgs e)
        {
            Utils.InvokeOn(this, () =>
            {
                var selected = lbox_Combos.SelectedItem;

                if (selected == null)
                    return;

                if (!combos.ContainsKey(selected.ToString()) && lbox_Combos.Items.Contains(selected))
                {
                    lbox_Combos.Items.Remove(selected);

                    return;
                }


                var combo = combos[selected.ToString()];

                lbox_ComboTriggers.Items.Clear();
                lbox_ComboTriggers.Items.AddRange(combo.Triggers.ToArray());

                lbox_ComboRotations.Items.Clear();
                lbox_ComboRotations.Items.AddRange(combo.Skills.ToArray());
            });
        }

        private void lbox_SkillsList_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selected = lbox_SkillsList.SelectedItem;

            if (selected == null)
                return;


            lbox_Conditions.Items.Clear();

            if (!conditions.ContainsKey(selected.ToString()))
                return;


            var conds = conditions[selected.ToString()].Select(c => c.Name);
            lbox_Conditions.Items.AddRange(conds.ToArray());
        }

        private void cmbox_ZoneMaps_SelectedIndexChanged(object sender, EventArgs e)
        {
            Utils.InvokeOn(this, () =>
            {
                int index = cmbox_ZoneMaps.SelectedIndex;

                // Clear box
                lbox_GpsPoints.Items.Clear();

                if (index == 0)
                    return;


                var mapName = cmbox_ZoneMaps.SelectedItem.ToString();
                var map = MapsHelper.GetMap(mapName);

                if (map == null)
                    return;


                var gps = new Gps(Host);

                switch (map.MapUseType)
                {
                    case MapUseType.Local:
                        gps.LoadDataBase(map.GetMapPath());
                        break;
                    case MapUseType.Internal:
                        gps.LoadDataBase(map.GetByteMap());
                        break;
                }


                var points = gps.GetAllGpsPoints().Where
                    (p => p.name.Contains("Fight")).Select(p => p.name + " : " + p.radius);

                if (points.Count() > 0)
                {
                    lbox_GpsPoints.Items.AddRange(points.ToArray());
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

        private void OnKeyDown(Keys key, bool isCtrlPressed, bool isShiftPressed, bool isAltPressed)
        {
            if (isCtrlPressed && key == Keys.S)
            {
                BaseBegin();
            }
        }

        #endregion
    }
}
