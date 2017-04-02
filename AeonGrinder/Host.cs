using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArcheBot.Bot.Classes;

namespace AeonGrinder
{
    using UI;
    using Modules;

    public class Host : Core
    {
        public UIContext UIContext;
        public BaseModule BaseModule;

        public static string PluginVersion()
        {
            return "1.0.0";
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
            Log("AeonGrinder v." + PluginVersion());


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
    }
}
