using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AeonGrinder.Data
{
    using UI;

    public class Statistics
    {
        private Window UI;

        #region Fields

        private int runTime;
        private int mobsKilled;
        private int expGained;
        private int goldEarned;
        private int deaths;
        private int suspectReports;
        private int whispersReceived;

        #endregion

        public Statistics(Window window)
        {
            UI = window;
            UI.ClearLootBag();
        }

        public int RunTime
        {
            get { return runTime; }
            set
            {
                (runTime) = value;

                var elapsed = new DateTime(TimeSpan.FromSeconds(RunTime).Ticks).ToString("HH:mm:ss");
                UI.UpdateLabel(UI.lbl_RunTime, elapsed);
            }
        }

        public int MobsKilled
        {
            get { return mobsKilled; }
            set
            {
                mobsKilled = value;
                UI.UpdateLabel(UI.lbl_MobsKilled, MobsKilled.ToString());
            }
        }

        public int ExpGained
        {
            get { return expGained; }
            set
            {
                expGained = value;
                UI.UpdateLabel(UI.lbl_ExpGained, ExpGained.ToString());
            }
        }

        public int GoldEarned
        {
            get { return goldEarned; }
            set
            {
                goldEarned = value;
                UI.UpdateLabel(UI.lbl_GoldEarned, ((long)goldEarned).GoldFormat().Format());
            }
        }

        public int Deaths
        {
            get { return deaths; }
            set
            {
                deaths = value;
                UI.UpdateLabel(UI.lbl_Deaths, Deaths.ToString());
            }
        }

        public int SuspectReports
        {
            get { return suspectReports; }
            set
            {
                suspectReports = value;
                UI.UpdateLabel(UI.lbl_SuspectReports, SuspectReports.ToString());
            }
        }

        public int WhispersReceived
        {
            get { return whispersReceived; }
            set
            {
                whispersReceived = value;
                UI.UpdateLabel(UI.lbl_WhispersReceived, WhispersReceived.ToString());
            }
        }
    }
}
