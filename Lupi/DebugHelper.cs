using System;

namespace Lupi
{
    internal static class DebugHelper
    {
        public static void Write(string message) =>
            System.Diagnostics.Debug.WriteLine($"{DateTime.UtcNow.ToString("H:mm:ss.fff")} - {message}");
    }
}
