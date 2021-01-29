﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClashSharp.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClashSharp
{
    class App : ApplicationContext
    {
        private readonly ILogger<App> logger;

        private readonly NotifyIcon notifyIcon;
        private readonly Clash clash;
        private readonly AppOptions _options;

        public App(
            ILogger<App> logger,
            Clash clash,
            ConfigManager configManager,
            IOptions<AppOptions> options
            )
        {
            this.logger = logger;
            this.clash = clash;
            _options = options.Value;

            notifyIcon = BuildNotifyIcon();

            logger.LogInformation("App started.");

            Task.Run(configManager.UpdateConfig);
            Task.Run(StartClash);
        }

        private NotifyIcon BuildNotifyIcon()
        {
            var menu = new ContextMenuStrip();

            var itemWeb = new ToolStripMenuItem("Dashboard");
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

        private void StartClash()
        {
            retry:
            try
            {
                clash.Start();
                clash.Exited += Clash_Exited;
            }
            catch (Clash.ServiceMissingException e)
            {
                logger.LogInformation(e, "Clash service not installed.");
                try
                {
                    Clash.InstallClashService();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Install Clash service failed.");
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

        private void Clash_Exited()
        {
            MessageBox.Show("Clash exited.");

            ExitApp();
        }

        private void OnWebClick(object? sender, EventArgs e)
        {
            var info = new ProcessStartInfo(_options.DashboardUrl)
            {
                UseShellExecute = true,
            };
            Process.Start(info);
        }

        private async void OnReloadClick(object? sender, EventArgs e)
        {
            var success = await ReloadConfig();
            if (success)
            {
                return;
            }

            MessageBox.Show("Reload config failed.");
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

    record AppOptions
    {
        public string DashboardUrl { get; set; } = "https://yacd.haishan.me/";
    }
}
