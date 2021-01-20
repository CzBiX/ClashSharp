using System;
using System.Windows.Forms;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;

using ClashSharp.UI;

namespace ClashSharp
{
    static class Program
    {
        static CommandLineBuilder BuildCommand()
        {
            RootCommand rootCommand = new RootCommand()
            {
                Handler = CommandHandler.Create<IHost>(RunApp),
            };

            rootCommand.AddCommand(new Command("install-task")
            {
                Handler = CommandHandler.Create(InstallTask),
            });

            rootCommand.AddCommand(new Command("run-clash")
            {
                Handler = CommandHandler.Create<IHost>(RunClash),
            });


            var builder = new CommandLineBuilder(rootCommand);

            return builder;
        }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            var cmd = BuildCommand();
            cmd.UseHost(BuildHost);

            return cmd.Build().Invoke(args);
        }

        private static void InstallTask()
        {
            TaskHelper.InstallTask(Application.ExecutablePath, "run-clash");
        }

        private static void RunClash(IHost host)
        {
            var clash = host.Services.GetRequiredService<Clash>();
            var form = new HideForm();
            Application.ThreadExit += (_, _) => clash.Stop();
            clash.Exited += (_, _) => form.Close();

            clash.Start(true);

            // HACK: for receive Exit event
            Application.Run(form);
        }

        private static void RunApp(IHost host)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var app = host.Services.GetRequiredService<App>();

            Application.Run(app);
        }

        private static IHostBuilder BuildHost(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                services.AddScoped<ClashApi>();
                services.AddSingleton(services =>
                {
                    var logger = services.GetRequiredService<ILogger<Clash>>();
                    var api = new Lazy<ClashApi>(() => services.GetRequiredService<ClashApi>());
                    return new Clash(logger, api, "clash-windows-amd64.exe", "clash-home", true);
                });
                services.AddSingleton<App>();
            });

            return builder;
        }
    }
}
