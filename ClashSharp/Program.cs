using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace ClashSharp
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var host = BuildHost();
            var app = host.Services.GetRequiredService<App>();

            Application.Run(app);
        }

        private static IHost BuildHost()
        {
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices(services =>
            {
                services.AddScoped<ClashApi>();
                services.AddSingleton(services =>
                {
                    var logger = services.GetRequiredService<ILogger<Clash>>();
                    var api = services.GetRequiredService<ClashApi>();
                    return new Clash(logger, api, "clash-windows-amd64.exe", "clash-home", true);
                });
                services.AddSingleton<App>();
            });

            return builder.Build();
        }
    }
}
