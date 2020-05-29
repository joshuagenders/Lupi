using System;
using System.Threading;
using System.Threading.Tasks;

namespace Yakka
{
    public class Runner : IRunner
    {
        private readonly IPlugin _plugin;

        private readonly ThreadControl _threadControl;

        public Runner(IPlugin plugin, ThreadControl threadControl)
        {
            _plugin = plugin;
            _threadControl = threadControl;
        }

        public void RunSetup()
        {
        }

        public async Task RunTestLoop(DateTime startTime, CancellationToken ct)
        {
            var threadName = $"worker_{Guid.NewGuid().ToString("N")}";
            bool testCompleted = false;
            while (!ct.IsCancellationRequested && !testCompleted)
            {
                DebugHelper.Write($"request task execution {threadName}");
                testCompleted = await _threadControl.RequestTaskExecution(startTime, ct);
                DebugHelper.Write($"task execution request returned {threadName}");
                if (!ct.IsCancellationRequested && !testCompleted)
                {
                    DebugHelper.Write($"test not complete - run method {threadName}");
                    _plugin.ExecuteTestMethod();
                    DebugHelper.Write($"method invoke complete {threadName}");
                }
                else
                {
                    DebugHelper.Write($"thread complete {threadName}");
                }
                //todo request should die
            }
        }
        public void RunTest(string threadName)
        {
            //todo how to handle async in runner
            

        }

        public void RunTeardown()
        {
            
        }
    }

    public interface IRunner
    {
        Task RunTestLoop(DateTime startTime, CancellationToken ct);
    }
}
