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
        public int Count { get; set; }
        public int PeriodErrorCount { get; set; }
        public int PeriodSuccessCount { get; set; }
        public double Mean { get; set; }
        public double Variance { get; set; }
        public double StandardDeviation { get; set; }
    }
}