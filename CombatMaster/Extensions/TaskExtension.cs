using System.Threading;

namespace CombatMaster
{
    public static class TaskExtension
    {
        public static bool IsAlive(this CancellationToken token)
        {
            return !token.IsCancellationRequested;
        }
    }
}
