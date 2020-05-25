using System;

namespace Yakka
{
    public class Runner : IRunner
    {
        public Config _config { get; }
        public Plugin _plugin { get; }
        public Runner(Plugin plugin, Config config)
        {
            _config = config;
            _plugin = plugin;
        }

        public void RunSetup()
        {

        }

        public void RunTest(string threadName)
        {
            //todo how to handle async in runner
            throw new NotImplementedException();
        }

        public void RunTeardown()
        {

        }
    }

    public interface IRunner
    {
        void RunTest(string threadName);
    }
}
