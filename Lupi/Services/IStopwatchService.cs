using System.Diagnostics;

namespace Lupi.Services {

    public interface IStopwatchService {
        Stopwatch GetStopwatch();
    }

    public class StopwatchService : IStopwatchService {
        public Stopwatch GetStopwatch() => new Stopwatch();
    }
}