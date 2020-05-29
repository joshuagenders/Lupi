using System;

namespace Yakka
{
    public class TestResult
    {
        public bool Passed { get; set; }
        public string Result { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}