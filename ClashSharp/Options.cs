using System.CommandLine;
using ClashSharp.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace ClashSharp
{
    static class Options
    {
        public static readonly Option<string?> WorkingDirectory = new(new[]{ "--cd" }, "Change working directory");

        public static void AddAppOptions(this IServiceCollection services)
        {
            services.AddOptions<FileLoggerOptions>()
                .BindConfiguration("FileLogger")
                .Configure<IHostEnvironment>((options, environment) =>
                {
                    var name = environment.ApplicationName!;
                    options.Name = WindowsServiceHelpers.IsWindowsService() ? name + "-service" : name;
                });

            services.AddOptions<AppOptions>()
                .BindConfiguration("App");
            services.AddOptions<ClashOptions>()
                .BindConfiguration("Clash");
            services.AddOptions<SubscriptionOptions>()
                .BindConfiguration("Subscription");
        }
    }
}
