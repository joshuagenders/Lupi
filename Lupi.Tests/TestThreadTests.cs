﻿using FluentAssertions;
using System;
using Xunit;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using System.Diagnostics;
using AutoFixture.Xunit2;
using Lupi.Core;

namespace Lupi.Tests
{
    public class TestThreadTests
    {
        [Theory]
        [InlineAutoMoqData()]
        public async Task WhenThreadControlSignals_ThenThreadStops(
            [Frozen]Mock<ITokenManager> tokenManager,
            TestThread testThread)
        {
            var cts = new CancellationTokenSource();
            var longTime = 100000;
            cts.CancelAfter(longTime);
            tokenManager
                .Setup(t => t.RequestTaskExecution(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var watch = new Stopwatch();
            watch.Start();
            await testThread.Run(cts.Token);
            watch.Stop();
            watch.ElapsedMilliseconds.Should().BeLessThan(longTime - 100);
        }

        //todo 
        // WhenExceptionsReturned_ThenTestsAreFailed
        //  WhenTaskReturnsFalse_ThenTestsAreFailed
        // WhenTaskReturnsNull_Then?
    }
}
