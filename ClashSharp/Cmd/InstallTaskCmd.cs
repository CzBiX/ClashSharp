using System.CommandLine;
using System.CommandLine.Invocation;
using System.Windows.Forms;

namespace ClashSharp.Cmd
{
    public class InstallTaskCmd : Command
    {
        public InstallTaskCmd() : base("install-task")
        {
            Handler = CommandHandler.Create(Run);
        }
        
        private static void Run()
        {
            TaskHelper.InstallTask(Application.ExecutablePath, "run-clash");
        }
    }
}