using System;

namespace AeonGrinder
{
    public class StopException : Exception
    {
        public StopException(string message) : base(message)
        {
        }
    }
}
