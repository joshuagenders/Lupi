namespace Lupi.Services {
    public interface ITimeService {
        DateTime Now ();
    }

    public class TimeService : ITimeService {
        public DateTime Now () => DateTime.UtcNow;
    }
}