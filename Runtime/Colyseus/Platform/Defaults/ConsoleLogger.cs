using System;

namespace Colyseus
{
    public class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        public void LogWarning(string message)
        {
            Console.WriteLine("[WARNING] " + message);
        }

        public void LogError(string message)
        {
            Console.Error.WriteLine("[ERROR] " + message);
        }
    }
}
