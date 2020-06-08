namespace Lupi.Listeners
{
    public class AggregatedResult
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public double MovingAverage { get; set; }
        public double PeriodMin { get; set; }
        public double PeriodMax { get; set; }
        public double PeriodAverage { get; set; }

        public double PeriodLength { get; set; }
    }
}