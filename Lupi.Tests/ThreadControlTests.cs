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
        // [Theory]
        // [InlineAutoMoqData(1)]
        public async Task WhenX_ThenY(int x, ThreadControl threadControl)
        {
            // var cts = new CancellationTokenSource();
            // await threadControl.RequestTaskExecution(DateTime.Now, cts.Token);
        }
    }
}