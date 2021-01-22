using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.IO;
using ClashSharp.Cmd;
using ClashSharp.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClashSharp
{
    static class Program
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
                services.AddTransient(typeof(Lazy<>), typeof(Lazier<>));
                services.AddSingleton<ILoggerProvider, FileLoggerProvider>();

                services.AddAppOptions();

                services.AddScoped<ClashApi>();
                services.AddSingleton<Clash>();
                services.AddSingleton<App>();
            });

            return builder;
        }

        private class Lazier<T> : Lazy<T> where T : class
        {
            public Lazier(IServiceProvider provider) : base(provider.GetRequiredService<T>) {}
        }
    }
}
