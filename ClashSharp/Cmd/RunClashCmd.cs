using System.CommandLine;
using System.CommandLine.Invocation;
using System.Windows.Forms;
using ClashSharp.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClashSharp.Cmd
{
    public class RunClashCmd : Command
    {
        public RunClashCmd() : base("run-clash")
        {
            Handler = CommandHandler.Create<IHost>(Run);
        }
        
        private static void Run(IHost host)
        {
            var clash = host.Services.GetRequiredService<Clash>();
            var form = new HideForm();
            Application.ThreadExit += (_, _) => clash.Stop();
            clash.Exited += (_, _) => form.Close();

            clash.Start(true);

            // HACK: for receive Exit event
            Application.Run(form);
        }
    }
}