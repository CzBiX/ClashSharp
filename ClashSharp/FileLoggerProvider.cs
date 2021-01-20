using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ClashSharp
{
    class FileLoggerProvider : ILoggerProvider
    {
        public string FileName;
        private readonly Lazy<StreamWriter> file;

        public FileLoggerProvider(string fileName)
        {
            FileName = fileName;

            file = new Lazy<StreamWriter>(() =>
            {
                return new StreamWriter(fileName)
                {
                    AutoFlush = true
                };
            });
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(this, categoryName);
        }

        public StreamWriter GetStream()
        {
            return file.Value;
        }

        public void Dispose()
        {
            if (file.IsValueCreated)
            {
                file.Value.Close();
            }
        }

        private class FileLogger : ILogger
        {
            public readonly FileLoggerProvider Provider;
            public readonly string CategoryName;

            public FileLogger(FileLoggerProvider provider, string categoryName)
            {
                this.Provider = provider;
                this.CategoryName = categoryName;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                throw new NotImplementedException();
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                var msg = formatter.Invoke(state, exception);
                var line = $"[{logLevel}][{eventId.Name}] {msg}";
                Provider.GetStream().WriteLine(line);
            }
        }
    }
}
