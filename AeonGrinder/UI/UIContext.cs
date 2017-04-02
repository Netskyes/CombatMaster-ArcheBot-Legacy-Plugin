using System;
using System.Threading;
using System.Windows.Forms;

namespace AeonGrinder.UI
{
    public class UIContext
    {
        private Form ui;
        private Thread uiThread;

        public Form Instance
        {
            get { return ui; }
        }

        public UIContext(Form window)
        {
            ui = window;
        }

        private void Run()
        {
            Application.EnableVisualStyles();
            Application.Run(ui);
        }

        public void Load()
        {
            if (ui != null)
            {
                uiThread = new Thread(Run);
                uiThread.SetApartmentState(ApartmentState.STA);
                uiThread.Start();
            }
        }

        public void Unload()
        {
            if (ui != null && !ui.IsDisposed)
            {
                try
                {
                    ui.Invoke(new Action(() => ui.Close()));
                    ui.Invoke(new Action(() => ui.Dispose()));
                }
                catch { }

                try
                {
                    if(uiThread != null && uiThread.ThreadState == ThreadState.Running)
                    {
                        uiThread.Abort();
                        uiThread.Join();
                    }
                }
                catch { }
            }
        }
    }
}
