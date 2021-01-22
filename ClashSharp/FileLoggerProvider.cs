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
            return new FileLogger(this, categoryName, file);
        }

        public void Dispose()
        {
            if (file.IsValueCreated)
            {
                file.Value.Close();
            }
        }
    }

    class FileLogger : ILogger
    {
        public readonly FileLoggerProvider Provider;
        public readonly string CategoryName;

        private readonly Lazy<StreamWriter> _writer;

        public FileLogger(FileLoggerProvider provider, string categoryName, Lazy<StreamWriter> writer)
        {
            Provider = provider;
            CategoryName = categoryName;

            _writer = writer;
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
            _writer.Value.WriteLine(line);
        }

        private static char GetLevelChar(LogLevel logLevel)
        {
            return logLevel.ToString()[0];
        }
    }

    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    record FileLoggerOptions
    {
        public string Path { get; set; } = "logs";
    }
}
