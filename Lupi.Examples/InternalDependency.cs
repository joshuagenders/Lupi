using System;

namespace Lupi.Examples
{
    public interface IInternalDependency 
    {
        int GetData();
        int GetDataIntensive();
    }

    public class InternalDependency : IInternalDependency
    {
        private static readonly Random _r = new Random();
        public int GetData()
        {
            return _r.Next(1, 100);
        }

        public int GetDataIntensive() =>
            FindPrimeNumber(GetData() * 10 + GetData());

        private int FindPrimeNumber(int n)
        {
            int count = 0;
            int a = 2;
            while (count<n)
            {
                long b = 2;
                int prime = 1;
                while (b * b <= a)
                {
                    if(a % b == 0)
                    {
                        prime = 0;
                        break;
                    }
                    b++;
                }
                if (prime > 0)
                {
                    count++;
                }
                a++;
            }
            return --a;
        }
    }
}
