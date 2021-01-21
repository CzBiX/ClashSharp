using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.IO;
using ClashSharp.Cmd;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClashSharp
{
    internal static class Program
    {
        private static CommandLineBuilder BuildCommand()
        {
            RootCommand rootCommand = new MainCmd()
            {
                new InstallTaskCmd(),
                new RunClashCmd(),
            };

            var builder = new CommandLineBuilder(rootCommand);
            builder.AddGlobalOption(Options.WorkingDirectory);

            return builder;
        }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        public static int Main(string[] args)
        {
            var cmd = BuildCommand();
            cmd.UseDefaults();
            cmd.UseMiddleware(invocation =>
            {
                var dir = invocation.ParseResult.ValueForOption(Options.WorkingDirectory);
                if (dir != null)
                {
                    Directory.SetCurrentDirectory(dir);
                }
            });
            cmd.UseHost(BuildHost);

            return cmd.Build().Invoke(args);
        }

        private static IHostBuilder BuildHost(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                services.AddOptions<FileLoggerProvider.FileLoggerOptions>()
                    .BindConfiguration("FileLogger");
                services.AddSingleton<ILoggerProvider, FileLoggerProvider>();

                services.AddScoped<ClashApi>();
                services.AddSingleton(serviceProvider =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<Clash>>();
                    var api = new Lazy<ClashApi>(serviceProvider.GetRequiredService<ClashApi>);
                    return new Clash(logger, api, "clash-windows-amd64.exe", "clash-home", true);
                });
                services.AddSingleton<App>();
            });

            return builder;
        }
    }
}
