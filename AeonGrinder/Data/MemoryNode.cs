using System.Threading;
using System.Threading.Tasks;

namespace AeonGrinder.Data
{
    class MemoryNode
    {
        #region Fields

        private int accessNum;
        private int tickUnlockNum;

        private CancellationTokenSource ts;
        private CancellationToken token;

        private bool isLocked;
        private readonly object nodeLock = new object();

        #endregion

        public Task UnlockTask { get; private set; }

        public bool IsLocked
        {
            get
            {
                lock (nodeLock) { return isLocked; }
            }
            set
            {
                lock (nodeLock) { isLocked = value; }
            }
        }

        public bool IsTickUnlock { get; set; }


        public int LockWhen { get; set; }
        public int UnlockWhen { get; set; }

        public int AccessTimes()
        {
            accessNum++;
            return accessNum;
        }

        public int TickUnlockTimes()
        {
            tickUnlockNum++;
            return tickUnlockNum;
        }


        public void Lock(int time, bool tickUnlock)
        {
            if (IsLocked)
                return;

            IsLocked = true;
            IsTickUnlock = tickUnlock;

            if (IsTickUnlock)
                return;

            ts = new CancellationTokenSource();
            token = ts.Token;

            UnlockTask = RunTask(time);
        }

        public void Unlock()
        {
            if (!IsLocked)
                return;

            IsLocked = false;
            
            if (!IsTickUnlock && UnlockTask != null && UnlockTask.Status == TaskStatus.Running)
            {
                ts.Cancel();
            }


            accessNum = 0;
            tickUnlockNum = 0;
            IsTickUnlock = false;
        }

        public bool IsLockedCheck()
        {
            if (IsTickUnlock)
            {
                if (TickUnlockTimes() > UnlockWhen)
                {
                    Unlock();
                }
            }

            return IsLocked;
        }

        private Task RunTask(int ms)
        {
            return Task.Run(() =>
            {
                Utils.Delay(ms, token); Unlock();

            }, token);
        }
    }
}
