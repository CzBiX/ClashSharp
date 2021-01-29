using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClashSharp
{
    [ProviderAlias("File")]
    class FileLoggerProvider : ILoggerProvider
    {
        private readonly Lazy<StreamWriter> file;
        private readonly FileLoggerOptions _options;

        public FileLoggerProvider(
            IOptions<FileLoggerOptions> options
        )
        {
            _options = options.Value;

            file = new Lazy<StreamWriter>(CreateWriter);
        }

        private string OldFileName => _options.Name + "-old.log";

        private string FileName => _options.Name + ".log";

        private string FilePath => Path.Join(_options.Path, FileName);

        private StreamWriter CreateWriter()
        {
            var logFileInfo = new FileInfo(FilePath);
            var directoryName = logFileInfo.DirectoryName;
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName!);
            }

            if (logFileInfo.Exists && logFileInfo.Length >= 0x4000000)
            {
                logFileInfo.MoveTo(Path.Join(directoryName, OldFileName), true);
            }

            return new StreamWriter(FilePath, true)
            {
                AutoFlush = true
            };
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

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
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
        public string? Name { get; set; }
    }
}
