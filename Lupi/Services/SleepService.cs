namespace Lupi.Services {
    public interface ISleepService {
        Task WaitFor(TimeSpan waitTime, CancellationToken ct);
    }

    public class SleepService : ISleepService {
        public async Task WaitFor(TimeSpan waitTime, CancellationToken ct) => await Task.Delay(waitTime, ct);
    }
}