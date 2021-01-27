using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClashSharp.Cmd;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32.TaskScheduler;
using Action = System.Action;
using Task = Microsoft.Win32.TaskScheduler.Task;

namespace ClashSharp.Core
{
    class Clash
    {
        private readonly ILogger<Clash> logger;
        private readonly Lazy<ClashApi> api;

        private Process? process;
        private Task? task;
        private CancellationTokenSource? taskWatcherToken;

        private readonly ClashOptions _options;
        private readonly ConfigManager _configManager;

        public event Action? Exited;

        public class TaskMissingException : Exception
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

        private void StartTask()
        {
            var t = TaskHelper.GetTask();
            if (t == null)
            {
                throw new TaskMissingException();
            }

            if (t.State != TaskState.Ready)
            {
                throw new Exception("Invalid task status.");
            }

            t.Run();

            taskWatcherToken = new CancellationTokenSource();
            System.Threading.Tasks.Task.Run(async () =>
            {
                while (!taskWatcherToken.IsCancellationRequested)
                {
                    if (t.State != TaskState.Running)
                    {
                        Exited?.Invoke();
                        return;
                    }

                    await System.Threading.Tasks.Task.Delay(3000);
                }
            }, taskWatcherToken.Token);

            task = t;
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
            if (!_configManager.ConfigReadyEvent.IsSet)
            {
                logger.LogInformation("Waiting for config.");
                _configManager.ConfigReadyEvent.Wait();
            }

            logger.LogInformation("Start clash.");
            if (NeedAdmin && !forceProcessMode)
            {
                StartTask();
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
            else if (task != null)
            {
                taskWatcherToken!.Cancel();
                task.Stop();
                task = null;
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

        public static void InstallClashTask()
        {
            var info = new ProcessStartInfo(Application.ExecutablePath, InstallTaskCmd.Name)
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
