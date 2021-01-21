using System.CommandLine;
using System.CommandLine.Invocation;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClashSharp.Cmd
{
    public class MainCmd : RootCommand
    {
        public MainCmd()
        {
            Handler = CommandHandler.Create<IHost>(Run);
        }

        private static void Run(IHost host)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var app = host.Services.GetRequiredService<App>();

            Application.Run(app);
        }
    }
}