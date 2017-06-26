using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArcheBot.Bot.Classes;

namespace CombatMaster
{
    using UI;
    using Modules;

    public class Host : Core
    {
        public UIContext UIContext;
        public BaseModule BaseModule;

        public static string PluginVersion()
        {
            return "1.1.4";
        }

#if !DEBUG
        public static bool isReleaseVersion = true;
        public static int storePluginId = 17;
#endif

        private bool initStop = false;


        public bool IsGameReady()
        {
            return me != null && gameState == GameState.Ingame;
        }


        private void Initialize()
        {
            Paths.Validate();

            UIContext = new UIContext(new Window(this));
            BaseModule = new BaseModule(this);


            // Console Text Color
            LogSetColor(System.Drawing.Color.White);
        }

        public void PluginRun()
        {
            if (!IsGameReady())
            {
                Log("Loading or not in game...");

                while (!IsGameReady()) Utils.Sleep(50);
            }

            ClearLogs();
            Log("CombatMaster v." + PluginVersion());


            Debug();
            Initialize();


            UIContext.Load();

            try
            {
                while (!initStop) Utils.Sleep(50);
            }
            catch
            {
                // Skip
            }
            finally
            {
                UIContext.Unload();
            }
        }

        public void PluginStop()
        {
            initStop = true;

            BaseModule.CancelActions();
        }


        // DEBUG
        private void Debug()
        {
        }

        #region DEBUG DUMPS

        private void DumpSkills()
        {
            foreach (var s in me.getSkills())
            {
                Log(s.name + " " + s.id);
            }
        }

        private void DumpEqItems()
        {
            foreach (var i in me.getAllEquipedItems())
            {
                Log(i.name + " " + i.id + " " + i.armorType + " " + i.weaponType);
            }
        }

        private void DumpMountsAndPets()
        {
            foreach (var m in sqlCore.sqlNpcs.Where(n => n.Value.mountSkills.Count > 0).OrderBy(n => n.Value.npcKindId))
            {
                IEnumerable<KeyValuePair<uint, ArcheBot.SQL.SqlItem>> items;

                try
                {
                    items = sqlCore.sqlItems.Where(i => i.Value.name == m.Value.name);
                }
                catch
                {
                    continue;
                }


                foreach (var item in items)
                {
                    string temp = string.Format(@"new Slave() {{ ItemId = {0}, Id = {1}, KindId = {2} }},", item.Value.id, m.Value.id, m.Value.npcKindId);

                    System.IO.File.AppendAllText(Paths.Plugin + "slaves.txt", temp + Environment.NewLine);
                }
            }
        }

        #endregion
    }
}
