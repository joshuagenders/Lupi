using FluentAssertions;
using Lupi.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Lupi.Tests {
    public class ThreadControlTests 
    {
        // WhenDurationSpecified_ThenDurationIsObserved
        // WhenMoreIterationsThanDurationAllows_ThenTestExitsEarly (end time is preferred over iteration count)
        // WhenMoreIterationsThanSingleThreadAllows_ThenThreadsAdapt
        // WhenThinkTimeIsSpecified_ThenWaitIsObserved
        // WhenRampDownConcurrencyIsSpecified_ThenThreadsRpsDecreases
    }
}