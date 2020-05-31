using System;

namespace Lupi
{
    public class TestResult
    {
        public bool Passed { get; set; }
        public string Result { get; set; }
        public TimeSpan Duration { get; set; }
    }
}