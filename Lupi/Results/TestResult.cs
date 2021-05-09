using System;

namespace Lupi.Results
{
    public class TestResult
    {
        public string ThreadName { get; set; }
        public bool Passed { get; set; }
        public string Result { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime FinishedTime { get; set; }
    }
}