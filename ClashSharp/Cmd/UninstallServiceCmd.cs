using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.ComponentModel;
using ClashSharp.Core;
using ClashSharp.Native;
using static ClashSharp.Native.ServiceMethods;

namespace ClashSharp.Cmd
{
    public class UninstallServiceCmd : Command
    {
        public new const string Name = "uninstall-service";

        public UninstallServiceCmd() : base(Name)
        {
            Handler = CommandHandler.Create(Run);
        }

        private static void Run()
        {
            const string serviceName = Clash.ServiceName;

            using var manager = OpenSCManager(null, null, ScmAccess.ScManagerAllAccess)
                .AsSafeHandle(CloseServiceHandle);
            if (manager.IsInvalid)
            {
                throw new Exception("Open service manager failed", new Win32Exception());
            }

            using var service = OpenService(manager, serviceName, ServiceAccess.ServiceAllAccess)
                .AsSafeHandle(CloseServiceHandle);

            if (service.IsInvalid)
            {
                throw new Exception("Open service failed.", new Win32Exception());
            }

            if (!DeleteService(service))
            {
                throw new Exception("Could not delete service.", new Win32Exception());
            }
        }
    }
}
