using System.CommandLine;
using System.CommandLine.Invocation;
using ClashSharp.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClashSharp.Cmd
{
    public class RunClashCmd : Command
    {
        public new const string Name = "run-clash";

        public RunClashCmd() : base(Name)
        {
            Handler = CommandHandler.Create<IHost>(Run);
        }

        private static void Run(IHost host)
        {
            var serviceProvider = host.Services;
            var clash = serviceProvider.GetRequiredService<Clash>();
            var applicationLifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();

            applicationLifetime.ApplicationStopping.Register(() => clash.Stop());

            clash.Start(true);
            clash.WaitForExit();
        }
    }
}
