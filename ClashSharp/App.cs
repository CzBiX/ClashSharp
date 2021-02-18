using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using ClashSharp.Core;
using ClashSharp.Native;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClashSharp
{
    class App : ApplicationContext
    {
        private const int AppIconIndex = 135;
        private readonly ILogger<App> logger;

        private readonly NotifyIcon notifyIcon;
        private readonly Clash clash;
        private readonly ClashApi _api;
        private readonly AppOptions _options;

        public App(
            ILogger<App> logger,
            Clash clash,
            ClashApi clashApi,
            ConfigManager configManager,
            IOptions<AppOptions> options,
            IHostEnvironment environment
        )
        {
            this.logger = logger;
            this.clash = clash;
            _api = clashApi;
            _options = options.Value;

            notifyIcon = BuildNotifyIcon();

            logger.LogInformation("App started.");

            if (environment.IsDevelopment())
            {
                return;
            }

            Task.Run(configManager.UpdateConfig);
            Task.Run(StartClash);
        }

        private ContextMenuStrip BuildContextMenu()
        {
            var menu = new ContextMenuStrip();

            var itemWeb = new ToolStripMenuItem("Dashboard");
            itemWeb.Font = new Font(itemWeb.Font, FontStyle.Bold);
            itemWeb.Click += OnWebClick;

            var itemReload = new ToolStripMenuItem("Reload")
            {
                // We already support automatic reload config
                Visible = false,
            };
            itemReload.Click += OnReloadClick;

            var itemAbout = new ToolStripMenuItem("About");
            itemAbout.Click += OnAboutClick;

            var itemExit = new ToolStripMenuItem("Exit");
            itemExit.Click += OnExitClick;

            menu.Items.AddRange(new ToolStripItem[]
            {
                itemWeb,
                new ToolStripSeparator(),
                itemAbout,
                itemExit,
            });

            return menu;
        }

        private NotifyIcon BuildNotifyIcon()
        {
            var icon = new NotifyIcon()
            {
                Icon = Shell32.GetShell32Icon(iconIndex: AppIconIndex),
                Text = "ClashSharp",
                ContextMenuStrip = BuildContextMenu(),
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

        private async void OnAboutClick(object? sender, EventArgs e)
        {
            var appVersion = Application.ProductVersion!;
            var versionInfo = await _api.GetVersion();
            var msg = $"ClashSharp version: {appVersion}\nClash version: {versionInfo.Version}";
            if (versionInfo.Premium)
            {
                msg += " (Premium)";
            }

            MessageBox.Show(msg, "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private Task<bool> ReloadConfig()
        {
            return clash.ReloadConfig();
        }

        public void ExitApp()
        {
            logger.LogInformation("Exit App.");

            clash.Exited -= Clash_Exited;
            clash.Stop();

            notifyIcon.Visible = false;

            ExitThread();
        }
    }

    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    record AppOptions
    {
        public string DashboardUrl { get; set; } = "https://yacd.haishan.me/";
    }
}
