using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Security;
using System.Threading.Tasks;

namespace ClashSharp
{
    class Clash
    {
        private readonly ILogger<Clash> logger;
        private readonly ClashApi api;

        private Process? process;
        private readonly string exePath;
        private readonly string homePath;
        private readonly bool needAdmin;

        public event EventHandler? Exited;

        public Clash(ILogger<Clash> logger, ClashApi api, string exePath, string homePath, bool needAdmin)
        {
            this.logger = logger;
            this.api = api;

            this.exePath = exePath;
            this.homePath = homePath;
            this.needAdmin = needAdmin;
        }

        public void Start()
        {
            var info = new ProcessStartInfo(exePath, $"-d {homePath}")
            {
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            if (needAdmin)
            {
                info.Verb = "runas";
                info.UseShellExecute = true;
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

        public void Stop()
        {
            if (process != null)
            {
                process.Kill();
                process.WaitForExit();
                process = null;
            }
        }

        public async Task<bool> ReloadConfig()
        {
            try
            {
                await api.ReloadConfig();
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

            if (process != null)
            {
                process.Close();
                process = null;
            }

            Exited?.Invoke(sender, e);
        }
    }
}
