﻿using System;

namespace Lupi.Examples
{
    public interface IInternalDependency 
    {
        int GetData();
    }

    public class InternalDependency : IInternalDependency
    {
        private static readonly Random _r = new Random();
        public int GetData()
        {
            return _r.Next(1, 100);
        }
    }
}