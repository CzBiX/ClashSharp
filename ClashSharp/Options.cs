using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ClashSharp
{
    static class Options
    {
        public static readonly Option<string?> WorkingDirectory = new(new string[]{ "-d", "--cd" }, "Change working directory");

        public static void AddAppOptions(this IServiceCollection services)
        {
            services.AddOptions<FileLoggerProvider.FileLoggerOptions>()
                .BindConfiguration("FileLogger");
            services.AddOptions<Clash.ClashOptions>()
                .BindConfiguration("Clash");
        }
    }
}
