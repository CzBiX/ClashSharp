using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ClashSharp
{
    class App : ApplicationContext
    {
        private readonly ILogger<App> logger;

        private readonly NotifyIcon notifyIcon;
        private readonly Clash clash;

        public App(ILogger<App> logger, Clash clash)
        {
            this.logger = logger;
            this.clash = clash;

            notifyIcon = BuildNotifyIcon();

            logger.LogInformation("App started.");

            Task.Run(StartClash);
        }

        private NotifyIcon BuildNotifyIcon()
        {
            var menu = new ContextMenuStrip();

            var itemWeb = new ToolStripMenuItem("Web");
            itemWeb.Font = new Font(itemWeb.Font, FontStyle.Bold);
            itemWeb.Click += OnWebClick;
            var itemReload = new ToolStripMenuItem("Reload");
            itemReload.Click += OnReloadClick;
            var itemExit = new ToolStripMenuItem("Exit");
            itemExit.Click += OnExitClick;

            menu.Items.AddRange(new ToolStripItem[] {
                itemWeb,
                itemReload,
                itemExit,
            });

            var icon = new NotifyIcon()
            {
                Icon = SystemIcons.Application,
                Text = "ClashSharp",
                ContextMenuStrip = menu,
                Visible = true,
            };

            icon.DoubleClick += OnWebClick;

            return icon;
        }

        private static void InstallClashTask()
        {
            var info = new ProcessStartInfo(Application.ExecutablePath, "install-task")
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

        private void StartClash()
        {
            retry:
            try
            {
                clash.Start();
                clash.Exited += Clash_Exited;
            }
            catch (Clash.TaskMissingException e)
            {
                logger.LogInformation(e, "Clash task not installed.");
                try
                {
                    InstallClashTask();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Install Clash task failed.");
                    ExitApp();
                    return;
                }

                goto retry;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Start Clash failed.");
                MessageBox.Show("Start Clash failed.\n" + e.Message);
                ExitApp();
            }
        }

        private void Clash_Exited(object? sender, EventArgs e)
        {
            MessageBox.Show("Clash exited.");

            ExitApp();
        }

        private void OnWebClick(object? sender, EventArgs e)
        {
            var info = new ProcessStartInfo("https://yacd.haishan.me/")
            {
                UseShellExecute = true,
            };
            Process.Start(info);
        }

        private async void OnReloadClick(object? sender, EventArgs e)
        {
            await ReloadConfig();
        }

        private void OnExitClick(object? sender, EventArgs e)
        {
            ExitApp();
        }

        private Task<bool> ReloadConfig()
        {
            return clash.ReloadConfig();
        }

        private void ExitApp()
        {
            logger.LogInformation("Exit App.");

            clash.Exited -= Clash_Exited;
            clash.Stop();

            notifyIcon.Visible = false;

            ExitThread();
        }
    }
}
