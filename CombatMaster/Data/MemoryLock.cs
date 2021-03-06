﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CombatMaster.Data
{
    internal class MemLock
    {
        private Dictionary<string, MemoryNode> nodes = new Dictionary<string, MemoryNode>();
        private readonly object nodesLock = new object();

        private Dictionary<string, TimeNode> times = new Dictionary<string, TimeNode>();
        private readonly object timesLock = new object();

        public List<string> NodeLocks
        {
            get { return nodes.Keys.ToList(); }
        }

        public List<string> TimeLocks
        {
            get { return times.Keys.ToList(); }
        }

        public MemLock()
        {
        }


        public bool Lock(string name, int lockTime, int preLockNum = 0, bool isTickUnlock = false)
        {
            MemoryNode node;

            lock (nodesLock)
            {
                if (!NodeExists(name))
                {
                    node = new MemoryNode() { LockWhen = preLockNum };

                    if (isTickUnlock)
                    {
                        node.UnlockWhen = lockTime;
                    }

                    nodes.Add(name, node);
                }

                // Get node
                node = GetNode(name);
            }


            if (node.IsLocked)
                return false;

            if (node.AccessTimes() <= node.LockWhen)
                return false;


            node.Lock(lockTime, isTickUnlock);

            return (node.IsLocked);
        }

        public bool IsLocked(string name)
        {
            MemoryNode node;

            lock (nodesLock)
            {
                node = GetNode(name);
            }

            return (node != null) ? node.IsLockedCheck() : false;
        }


        public void TimeLock(string name, int seconds, Action action)
        {
            lock (timesLock)
            {
                if (times.ContainsKey(name))
                {
                    var node = times[name];
                    var unlockTime = node.LockTime.AddSeconds(node.LockLapse);

                    if (DateTime.Now >= unlockTime)
                    {
                        try
                        {
                            action.Invoke();
                        }
                        catch
                        {
                        }

                        UnlockTime(name);
                    }

                    return;
                }

                
                times.Add(name, new TimeNode() { LockTime = DateTime.Now, LockLapse = seconds });
            }
        }

        public void UnlockTime(string name)
        {
            lock (timesLock)
            {
                if (times.ContainsKey(name)) times.Remove(name);
            }
        }


        #region Helpers

        private bool NodeExists(string name) => nodes.ContainsKey(name);
        private MemoryNode GetNode(string name) => NodeExists(name) ? nodes[name] : null;

        #endregion
    }
}
