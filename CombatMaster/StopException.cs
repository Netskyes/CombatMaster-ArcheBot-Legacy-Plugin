using System;

namespace CombatMaster
{
    public class StopException : Exception
    {
        public StopException(string message) : base(message)
        {
        }
    }
}
