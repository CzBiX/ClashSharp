using System.CommandLine;
using System.CommandLine.Invocation;
using System.Windows.Forms;
using ClashSharp.Core;
using ClashSharp.UI;
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
            var clash = host.Services.GetRequiredService<Clash>();
            var form = new HideForm();
            Application.ThreadExit += (_, _) => clash.Stop();
            clash.Exited += () => form.Close();

            clash.Start(true);

            // HACK: for receive Exit event
            Application.Run(form);
        }
    }
}
