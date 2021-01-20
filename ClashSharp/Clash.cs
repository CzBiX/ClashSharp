using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace ClashSharp
{
    class Clash
    {
        private readonly ILogger<Clash> logger;
        private readonly Lazy<ClashApi> api;

        private Process? process;
        private Microsoft.Win32.TaskScheduler.Task? task;
        private CancellationTokenSource? taskWatcherToken;

        private readonly string exePath;
        private readonly string homePath;
        public readonly bool NeedAdmin;

        public event EventHandler? Exited;

        public class TaskMissingException : Exception { };

        public Clash(ILogger<Clash> logger, Lazy<ClashApi> api, string exePath, string homePath, bool needAdmin)
        {
            this.logger = logger;
            this.api = api;

            this.exePath = exePath;
            this.homePath = homePath;
            NeedAdmin = needAdmin;
        }

        private string BuildArguments()
        {
            return $"-d {homePath}";
        }

        private void StartTask()
        {
            var t = TaskHelper.GetTask();
            if (t == null)
            {
                throw new TaskMissingException();
            }

            if (t.State != Microsoft.Win32.TaskScheduler.TaskState.Ready)
            {
                throw new Exception("Invalid task status.");
            }

            t.Run();

            taskWatcherToken = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (!taskWatcherToken.IsCancellationRequested)
                {
                    if (t.State != Microsoft.Win32.TaskScheduler.TaskState.Running)
                    {
                        Exited?.Invoke(null, new EventArgs());
                        return;
                    }
                    await Task.Delay(3000);
                }
            }, taskWatcherToken.Token);

            task = t;
        }

        private void StartProcess()
        {
            var info = new ProcessStartInfo(exePath, BuildArguments())
            {
                CreateNoWindow = true,
            };

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
                await api.Value.ReloadConfig();
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
    }
}
