using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace ManualDemoDownloaderTests
{
    public class TestLogger : ILogger, IDisposable
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Logger.LogMessage($"[{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss zzz")}] [{logLevel}]#{eventId}:{formatter(state, exception)}");
        }

        public void Dispose()
        {
        }


        public void Log(string message, Exception e)
        {
            Log(LogLevel.Information, new EventId(), message, e, PrettyPrintExceptions);
        }

        public void Log(string message)
        {
            Log(LogLevel.Information, new EventId(), message, null, PrettyPrintExceptions);
        }

        private string PrettyPrintExceptions(string s, Exception e)
        {
            return $"{s}--{e}";
        }

    }

    public class TestLogger<T> : TestLogger,ILogger<T>
    {
    }
}
