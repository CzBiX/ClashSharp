using System;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using ClashSharp.Cmd;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClashSharp
{
    [ProviderAlias("File")]
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
            FilePath = Path.Join(optionsValue.Path, fileName);

            file = new Lazy<StreamWriter>(() =>
            {
                var dirCreated = false;
                openFile:
                try
                {
                    return new StreamWriter(FilePath, true)
                    {
                        AutoFlush = true
                    };
                }
                catch (DirectoryNotFoundException)
                {
                    if (dirCreated)
                    {
                        throw ;
                    }

                    Directory.CreateDirectory(optionsValue.Path);
                    dirCreated = true;
                    goto openFile;
                }
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

        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
        public record FileLoggerOptions
        {
            public string Path { get; set; } = "logs";
        }

        private class FileLogger : ILogger
        {
            public readonly FileLoggerProvider Provider;
            public readonly string CategoryName;

            public FileLogger(FileLoggerProvider provider, string categoryName)
            {
                Provider = provider;
                CategoryName = categoryName;
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
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                var msg = formatter.Invoke(state, exception);
                var time = DateTime.Now;
                var levelChar = GetLevelChar(logLevel);
                var eventLabel = string.IsNullOrEmpty(eventId.Name)
                    ? eventId.Id.ToString()
                    : $"{eventId.Id}:{eventId.Name}";
                var line = $"[{time}][{levelChar}][{CategoryName}][{eventLabel}] {msg}";
                Provider.GetStream().WriteLine(line);
            }

            private static char GetLevelChar(LogLevel logLevel)
            {
                return logLevel.ToString()[0];
            }
        }
    }
}
