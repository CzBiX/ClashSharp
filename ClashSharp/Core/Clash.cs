using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClashSharp.Cmd;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32.TaskScheduler;
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

        public event EventHandler? Exited;

        public class TaskMissingException : Exception { }

        public Clash(ILogger<Clash> logger, Lazy<ClashApi> api, IOptions<ClashOptions> options)
        {
            this.logger = logger;
            this.api = api;

            _options = options.Value;
        }

        private string[] BuildArguments()
        {
            return new []{
                "-d", _options.HomePath,
                "-f", ConfigPath,
            };
        }

        public string ConfigPath => Path.GetFullPath(Path.Join(_options.HomePath, "clash-sharp.yml"));

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
                        Exited?.Invoke(null, new EventArgs());
                        return;
                    }
                    await System.Threading.Tasks.Task.Delay(3000);
                }
            }, taskWatcherToken.Token);

            task = t;
        }

        public bool NeedAdmin => _options.EnableTun;

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
            if (NeedAdmin && !forceProcessMode)
            {
                StartTask();
            }
            else
            {
                StartProcess();
            }
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
                await api.Value.ReloadConfig(ConfigPath);
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

            Exited?.Invoke(sender, e);
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
