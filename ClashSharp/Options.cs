using System.CommandLine;
using ClashSharp.Core;
using Microsoft.Extensions.DependencyInjection;

namespace ClashSharp
{
    static class Options
    {
        public static readonly Option<string?> WorkingDirectory = new(new[]{ "--cd" }, "Change working directory");

        public static void AddAppOptions(this IServiceCollection services)
        {
            services.AddOptions<FileLoggerOptions>()
                .BindConfiguration("FileLogger");
            services.AddOptions<ClashOptions>()
                .BindConfiguration("Clash");
            services.AddOptions<SubscriptionOptions>()
                .BindConfiguration("Subscription");
        }
    }
}
