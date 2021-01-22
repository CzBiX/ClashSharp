using System.CommandLine;
using System.CommandLine.Invocation;
using System.Windows.Forms;

namespace ClashSharp.Cmd
{
    public class InstallTaskCmd : Command
    {
        public new const string Name = "install-task";

        public InstallTaskCmd() : base(Name)
        {
            Handler = CommandHandler.Create(Run);
        }
        
        private static void Run()
        {
            TaskHelper.InstallTask(Application.ExecutablePath, RunClashCmd.Name);
        }
    }
}
