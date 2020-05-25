using System;

namespace Yakka
{
    internal static class DebugHelper
    {
        public static void Write(string message) => 
            System.Diagnostics.Debug.Write($"{DateTime.UtcNow.ToString("H:mm:ss.fff")} - {message}");
    }
}
