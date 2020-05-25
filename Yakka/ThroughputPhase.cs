using System;
using System.Collections.Generic;

namespace Yakka
{
    public class ThroughputPhase
    {
        public TimeSpan Duration { get; set; }
        public double FromRps { get; set; }
        public double ToRps { get; set; }
        public double Rps { get; set; }

        public double GetPhaseTotalThroughput() =>
            Rps > 0
                ? Duration.TotalMilliseconds * Rps / 1000
                : Duration.TotalMilliseconds * (ToRps - FromRps) / 2 / 1000 + Duration.TotalMilliseconds * FromRps / 1000;

        public double GetPhaseThroughput(int millisecondsEllapsed)
        {
            if (Rps > 0)
            {
                return millisecondsEllapsed * Rps / 1000;
            }
            else if (FromRps == ToRps)
            {
                return millisecondsEllapsed * ToRps / 1000;
            }
            else if (FromRps < ToRps)
            {
                return (millisecondsEllapsed * (ToRps - FromRps)) / 2 + millisecondsEllapsed * FromRps / 1000;
            }
            else
            {
                var phaseTotal = Duration.TotalMilliseconds * (FromRps - ToRps) / 2 + Duration.TotalMilliseconds * ToRps;
                var millisecondsRemaining = Duration.TotalMilliseconds - millisecondsEllapsed;
                var remainingTotal = millisecondsRemaining * (FromRps - ToRps) / 2 + millisecondsRemaining * ToRps;
                return (phaseTotal - remainingTotal) / 1000;
            }
        }
    }

    public static class ThroughputPhaseExtensions
    {
        public static int TotalAllowedRequestsToNow(this List<ThroughputPhase> phases, int millisecondsEllapsed)
        {
            var totalTime = 0;
            var totalRequests = 0d;
            ThroughputPhase currentPhase = null;
            foreach (var phase in phases)
            {
                if (millisecondsEllapsed > totalTime + phase.Duration.TotalMilliseconds)
                {
                    break;
                }
                totalTime += Convert.ToInt32(phase.Duration.TotalMilliseconds);
                totalRequests += phase.GetPhaseTotalThroughput();
                currentPhase = phase;
            }
            return Convert.ToInt32(totalRequests + currentPhase?.GetPhaseThroughput(millisecondsEllapsed - totalTime) ?? 0);
        }
    }
}
