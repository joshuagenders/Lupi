using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Yakka
{
    public class Runner : IRunner
    {
        //todo how to handle async in adapter
        public void RunTest(string threadName)
        {
            throw new NotImplementedException();
        }
    }

    public interface IRunner
    {
        void RunTest(string threadName);
    }
}
