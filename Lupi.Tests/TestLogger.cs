using Lupi.Core;
using Microsoft.Extensions.Logging;
using System;

namespace Lupi.Tests
{
    class TestLogger<T> : ILogger<T>, IDisposable
    {
        public static TestLogger<T> Create() => new TestLogger<T>();
        private readonly Action<string> output = s => System.Diagnostics.Debug.WriteLine(s);

        public void Dispose()
        {
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter) => output(formatter(state, exception));

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable BeginScope<TState>(TState state) => this;
    }

    class TestLoggerFactory : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return TestLogger<TestThread>.Create();
        }

        public void Dispose()
        {
        }
    }
}
