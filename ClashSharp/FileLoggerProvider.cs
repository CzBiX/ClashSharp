using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClashSharp.Cmd;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClashSharp
{
    class FileLoggerProvider : ILoggerProvider
    {
        public readonly string FilePath;
        private readonly Lazy<StreamWriter> file;

        public FileLoggerProvider(
            IOptions<FileLoggerOptions> options,
            IHostEnvironment environment,
            ParseResult parseResult
            )
        {
            var optionsValue = options.Value;
            var isDaemon = parseResult.CommandResult.Command is RunClashCmd;
            var appName = environment.ApplicationName ?? "app";
            var fileName = (isDaemon ? appName + "-daemon" : appName) + ".log";
            FilePath = Path.Join(optionsValue.Path?.FullName, fileName);

            file = new Lazy<StreamWriter>(() => new StreamWriter(FilePath, true)
            {
                AutoFlush = true
            });
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(this, categoryName);
        }

        private StreamWriter GetStream()
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

        public record FileLoggerOptions
        {
            public DirectoryInfo? Path;
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
                var now = DateTime.Now;
                var line = $"[{now}][{logLevel}][{eventId.Name}] {msg}";
                Provider.GetStream().WriteLine(line);
            }
        }
    }
}
