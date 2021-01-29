using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows.Forms;
using ClashSharp.Core;
using ClashSharp.Native;
using static ClashSharp.Native.ServiceMethods;

namespace ClashSharp.Cmd
{
    public class InstallServiceCmd : Command
    {
        public new const string Name = "install-service";

        public InstallServiceCmd() : base(Name)
        {
            Handler = CommandHandler.Create(Run);
        }

        private static string QuotePath(string s)
        {
            return s.Contains(' ') ? $@"""{s}""" : s;
        }

        private static void SetServiceSecurity(SafeHandle service)
        {
            byte[] buf = Array.Empty<byte>();
            if (!QueryServiceObjectSecurity(service, SecurityInfos.DiscretionaryAcl, null, 0, out var bufSize))
            {
                if (Marshal.GetLastWin32Error() != Errors.InsufficientBuffer)
                {
                    throw new Exception("Could not query service object security size.", new Win32Exception());
                }

                buf = new byte[bufSize];
                if (!QueryServiceObjectSecurity(service, SecurityInfos.DiscretionaryAcl, buf, bufSize, out bufSize))
                {
                    throw new Exception("Could not query service object security.", new Win32Exception());
                }
            }

            var securityDescriptor = new RawSecurityDescriptor(buf, 0);
            var rawAcl = securityDescriptor.DiscretionaryAcl;
            var acl = new DiscretionaryAcl(false, false, rawAcl);

            var userSid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            const ServiceAccess accessMask = ServiceAccess.GenericRead | ServiceAccess.GenericExecute;
            acl.SetAccess(AccessControlType.Allow, userSid, (int) accessMask, InheritanceFlags.None,
                PropagationFlags.None);

            buf = new byte[acl.BinaryLength];
            acl.GetBinaryForm(buf, 0);

            securityDescriptor.DiscretionaryAcl = new RawAcl(buf, 0);
            buf = new byte[securityDescriptor.BinaryLength];
            securityDescriptor.GetBinaryForm(buf, 0);

            if (!SetServiceObjectSecurity(service, SecurityInfos.DiscretionaryAcl, buf))
            {
                throw new Exception("Could not set object security.", new Win32Exception());
            }
        }

        private static void Run()
        {
            const string serviceName = Clash.ServiceName;

            using var manager = OpenSCManager(null, null,
                    ScmAccess.ScManagerAllAccess)
                .AsSafeHandle(CloseServiceHandle);
            if (manager.IsInvalid)
            {
                throw new Exception("Open service manager failed", new Win32Exception());
            }

            var cmd = string.Join(' ',
                QuotePath(Application.ExecutablePath),
                $@"--cd {QuotePath(Directory.GetCurrentDirectory())}",
                $"{RunClashCmd.Name}"
            );

            using var service = CreateService(manager.DangerousGetHandle(), serviceName, "ClashSharp Service",
                    ServiceAccess.ServiceAllAccess,
                    ServiceType.ServiceWin32OwnProcess |
                    ServiceType.ServiceInteractiveProcess,
                    ServiceStart.ServiceDemandStart, ServiceError.ServiceErrorNormal,
                    cmd, null, null, null, null, null)
                .AsSafeHandle(CloseServiceHandle);

            if (service.IsInvalid)
            {
                throw new Exception("Install service failed.", new Win32Exception());
            }

            SetServiceSecurity(service);
        }
    }
}
