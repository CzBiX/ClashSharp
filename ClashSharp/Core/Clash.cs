using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClashSharp.Cmd;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClashSharp.Core
{
    class Clash
    {
        public const string ServiceName = "ClashSharpService";

        private readonly ILogger<Clash> logger;
        private readonly Lazy<ClashApi> api;

        private Process? process;
        private ServiceController? _sc;
        private CancellationTokenSource? _serviceWatcherToken;

        private readonly ClashOptions _options;
        private readonly ConfigManager _configManager;

        public event Action? Exited;

        public class ServiceMissingException : Exception
        {
        }

        public Clash(
            ILogger<Clash> logger,
            Lazy<ClashApi> api,
            IOptions<ClashOptions> options,
            ConfigManager configManager)
        {
            this.logger = logger;
            this.api = api;

            _options = options.Value;
            _configManager = configManager;
        }

        private IEnumerable<string> BuildArguments()
        {
            return new[]
            {
                "-d", _options.HomePath,
                "-f", _configManager.ConfigPath,
                "-ext-ctl", ClashApi.BaseAddr,
            };
        }

        private void StartService()
        {
            var sc = new ServiceController(ServiceName);
            if (sc == null)
            {
                throw new ServiceMissingException();
            }

            if (sc.Status != ServiceControllerStatus.Running)
            {
                if (sc.Status != ServiceControllerStatus.Stopped)
                {
                    throw new Exception("Invalid service status.");
                }

                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(3));
                if (sc.Status != ServiceControllerStatus.Running)
                {
                    throw new Exception("Clash service start failed.");
                }
            }

            _serviceWatcherToken = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (!_serviceWatcherToken.IsCancellationRequested)
                {
                    sc.Refresh();
                    if (sc.Status != ServiceControllerStatus.Running)
                    {
                        Exited?.Invoke();
                        return;
                    }

                    await Task.Delay(3000, _serviceWatcherToken.Token);
                }
            }, _serviceWatcherToken.Token);

            _sc = sc;
        }

        private bool NeedAdmin => _options.EnableTun;

        private void StartProcess()
        {
            var info = new ProcessStartInfo(_options.ExePath)
            {
                CreateNoWindow = !_options.ShowConsole,
            };
            foreach (var arg in BuildArguments())
            {
                info.ArgumentList.Add(arg);
            }

            var p = new Process()
            {
                EnableRaisingEvents = true,
                StartInfo = info,
            };
            p.Exited += Process_Exited;
            p.Start();

            process = p;
        }

        public void Start(bool forceProcessMode = false)
        {
            if (!forceProcessMode && !_configManager.ConfigReadyEvent.IsSet)
            {
                logger.LogInformation("Waiting for config.");
                _configManager.ConfigReadyEvent.Wait();
            }

            logger.LogInformation("Start clash.");
            if (NeedAdmin && !forceProcessMode)
            {
                StartService();
            }
            else
            {
                StartProcess();
            }

            _configManager.Updated += async () => await ReloadConfig();
        }

        public void Stop()
        {
            if (process != null)
            {
                process.Kill();
                process.Close();
                process = null;
            }
            else if (_sc != null)
            {
                _serviceWatcherToken!.Cancel();
                _sc.Stop();
                _sc = null;
            }
        }

        public async Task<bool> ReloadConfig()
        {
            try
            {
                await api.Value.ReloadConfig(_configManager.ConfigPath);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Reload config failed.");
                return false;
            }

            return true;
        }

        private void Process_Exited(object? sender, EventArgs e)
        {
            logger.LogInformation("Clash exited.");

            Stop();

            Exited?.Invoke();
        }

        public void WaitForExit()
        {
            process?.WaitForExit();
        }

        public static void InstallClashService()
        {
            var info = new ProcessStartInfo(Application.ExecutablePath, InstallServiceCmd.Name)
            {
                Verb = "runas",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            using var p = new Process()
            {
                EnableRaisingEvents = true,
                StartInfo = info,
            };

            p.Start();
            p.WaitForExit();
        }
    }

    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    record ClashOptions
    {
        public string ExePath { get; set; } = "clash-windows-amd64.exe";
        public string HomePath { get; set; } = "clash-home";
        public bool EnableTun { get; set; } = false;
        public bool ShowConsole { get; set; }
    }
}
