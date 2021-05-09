using System.Diagnostics;

namespace Lupi.Services {

    public interface IStopwatchFactory {
        Stopwatch GetStopwatch();
    }

    public class StopwatchFactory : IStopwatchFactory {
        public Stopwatch GetStopwatch() => new Stopwatch();
    }
}