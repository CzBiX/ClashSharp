using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace ClashSharp.Core
{
    class ConfigManager : IDisposable
    {
        private const string DefaultFileName = "clash-config.yml";
        private const string ConfigFileName = "clash-sharp.yml";

        private const string TunConfigs = @"
tun:
  enable: true
  stack: gvisor
  macOS-auto-detect-interface: true
  macOS-auto-route: true
  dns-hijack:
    - 198.18.0.2
";

        private readonly ILogger<ConfigManager> _logger;
        private readonly ClashOptions _options;
        private readonly SubscriptionManager _subscriptionManager;
        private readonly Lazy<IFileProvider> _fileProvider;
        private IDisposable? _fileWatcher;

        private bool _updateTaskStarted;

        public event Action? Updated;
        public readonly ManualResetEventSlim ConfigReadyEvent;

        public ConfigManager(
            ILogger<ConfigManager> logger,
            IOptions<ClashOptions> options,
            SubscriptionManager subscriptionManager
        )
        {
            _logger = logger;
            _options = options.Value;
            _subscriptionManager = subscriptionManager;
            _fileProvider = new Lazy<IFileProvider>(() => new PhysicalFileProvider(Directory.GetCurrentDirectory()));
            ConfigReadyEvent = new ManualResetEventSlim();
        }

        public void UpdateConfig()
        {
            if (_updateTaskStarted)
            {
                return;
            }

            _updateTaskStarted = true;

            string sourcePath;
            if (_subscriptionManager.HasSubscription)
            {
                _logger.LogInformation("Use subscription config.");
                Task.Run(() => _subscriptionManager.UpdateSubscription(CancellationToken.None));

                sourcePath = _subscriptionManager.SubscriptionPath;
                _subscriptionManager.Updated += () => UpdateConfig(sourcePath);
            }
            else
            {
                _logger.LogInformation("Use local config.");
                sourcePath = DefaultFileName;

                _fileWatcher = ChangeToken.OnChange(
                    () => _fileProvider.Value.Watch(sourcePath),
                    async () =>
                    {
                        await Task.Delay(250);
                        UpdateConfig(sourcePath);
                    });
            }

            if (!File.Exists(sourcePath))
            {
                return;
            }

            UpdateConfig(sourcePath);
        }

        private void UpdateConfig(string sourcePath)
        {
            var sourceFileTime = File.GetLastWriteTime(sourcePath);
            if (sourceFileTime == File.GetLastWriteTime(ConfigPath))
            {
                SetConfigReadyEvent();
                return;
            }

            var content = File.ReadAllLines(sourcePath);

            using var writer = new StreamWriter(ConfigPath);
            foreach (var line in content)
            {
                writer.WriteLine(line);
            }

            if (_options.EnableTun)
            {
                writer.Write(TunConfigs);
            }

            writer.Close();

            File.SetLastWriteTime(ConfigPath, sourceFileTime);

            SetConfigReadyEvent();
            Updated?.Invoke();
        }

        private void SetConfigReadyEvent()
        {
            if (!ConfigReadyEvent.IsSet)
            {
                ConfigReadyEvent.Set();
            }
        }

        public string ConfigPath => Path.GetFullPath(Path.Join(_options.HomePath, ConfigFileName));

        public void Dispose()
        {
            _fileWatcher?.Dispose();
        }
    }
}
