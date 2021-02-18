using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.IO;
using ClashSharp.Cmd;
using ClashSharp.Core;
using ClashSharp.DI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;

namespace ClashSharp
{
    static class Program
    {
        private static CommandLineBuilder BuildCommand()
        {
            RootCommand rootCommand = new MainCmd()
            {
                new InstallServiceCmd(),
                new UninstallServiceCmd(),
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
            cmd.UseMiddleware(invocation =>
            {
                ParseResult result = invocation.ParseResult;
                var dir = result.ValueForOption(Options.WorkingDirectory);
                if (dir != null)
                {
                    Directory.SetCurrentDirectory(dir);
                }
            });
            cmd.UseDefaults();
            cmd.UseHost(BuildHost, builder =>
            {
                if (WindowsServiceHelpers.IsWindowsService())
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveService<IHostLifetime, InvocationLifetime>();
                    });
                }
            });

            return cmd.Build().Invoke(args);
        }

        private static IHostBuilder BuildHost(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(ConfigureAppServices);
            builder.UseWindowsService(options => options.ServiceName = Clash.ServiceName);

            return builder;
        }

        private static void ConfigureAppServices(IServiceCollection services)
        {
            services.AddTransient(typeof(Lazy<>), typeof(Lazier<>));
            services.AddSingleton<ILoggerProvider, FileLoggerProvider>();

            services.AddAppOptions();

            services.AddSingleton<Clash>();
            services.AddSingleton<ClashApi>();
            services.AddSingleton<ConfigManager>();
            services.AddSingleton<SubscriptionManager>();
            services.AddSingleton<App>();
        }

        private class Lazier<T> : Lazy<T> where T : class
        {
            public Lazier(IServiceProvider provider) : base(provider.GetRequiredService<T>)
            {
            }
        }
    }
}
