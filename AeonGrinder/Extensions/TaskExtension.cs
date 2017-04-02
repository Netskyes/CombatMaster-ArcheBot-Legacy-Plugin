using System.Threading;

namespace AeonGrinder
{
    public static class TaskExtension
    {
        public static bool IsAlive(this CancellationToken token)
        {
            return !token.IsCancellationRequested;
        }
    }
}
