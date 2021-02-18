using System;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace ClashSharp.Native
{
    class ServiceMethods
    {
        public const string DllName = "advapi32.dll";

        [DllImport(DllName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr OpenSCManager(string? machineName, string? databaseName, ScmAccess dwAccess);

        [DllImport(DllName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateService(
            IntPtr hScManager,
            string lpServiceName,
            string lpDisplayName,
            ServiceAccess dwDesiredAccess,
            ServiceType dwServiceType,
            ServiceStart dwStartType,
            ServiceError dwErrorControl,
            string lpBinaryPathName,
            string? lpLoadOrderGroup,
            string? lpdwTagId,
            string? lpDependencies,
            string? lpServiceStartName,
            string? lpPassword);

        [DllImport(DllName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr OpenService(SafeHandle handle, string serviceName, ServiceAccess access);

        [DllImport(DllName, SetLastError = true)]
        public static extern bool DeleteService(SafeHandle handle);

        [DllImport(DllName, SetLastError = true)]
        public static extern bool QueryServiceObjectSecurity(SafeHandle serviceHandle, SecurityInfos secInfo,
            byte[]? secDesc, uint bufSize, out uint bufSizeNeeded);

        [DllImport(DllName, SetLastError = true)]
        public static extern bool SetServiceObjectSecurity(SafeHandle serviceHandle, SecurityInfos secInfo, byte[] secDesc);

        [DllImport(DllName, SetLastError = true)]
        public static extern bool CloseServiceHandle(IntPtr handle);

        [Flags]
        public enum ScmAccess : uint
        {
            /// <summary>
            /// Required to connect to the service control manager.
            /// </summary>
            ScManagerConnect = 0x00001,

            /// <summary>
            /// Required to call the CreateService function to create a service
            /// object and add it to the database.
            /// </summary>
            ScManagerCreateService = 0x00002,

            /// <summary>
            /// Required to call the EnumServicesStatusEx function to list the
            /// services that are in the database.
            /// </summary>
            ScManagerEnumerateService = 0x00004,

            /// <summary>
            /// Required to call the LockServiceDatabase function to acquire a
            /// lock on the database.
            /// </summary>
            ScManagerLock = 0x00008,

            /// <summary>
            /// Required to call the QueryServiceLockStatus function to retrieve
            /// the lock status information for the database.
            /// </summary>
            ScManagerQueryLockStatus = 0x00010,

            /// <summary>
            /// Required to call the NotifyBootConfigStatus function.
            /// </summary>
            ScManagerModifyBootConfig = 0x00020,

            /// <summary>
            /// Includes STANDARD_RIGHTS_REQUIRED, in addition to all access
            /// rights in this table.
            /// </summary>
            ScManagerAllAccess = AccessMask.StandardRightsRequired |
                                 ScManagerConnect |
                                 ScManagerCreateService |
                                 ScManagerEnumerateService |
                                 ScManagerLock |
                                 ScManagerQueryLockStatus |
                                 ScManagerModifyBootConfig,

            GenericRead = AccessMask.StandardRightsRead |
                          ScManagerEnumerateService |
                          ScManagerQueryLockStatus,

            GenericWrite = AccessMask.StandardRightsWrite |
                           ScManagerCreateService |
                           ScManagerModifyBootConfig,

            GenericExecute = AccessMask.StandardRightsExecute |
                             ScManagerConnect | ScManagerLock,

            GenericAll = ScManagerAllAccess,
        }

        /// <summary>
        /// Access to the service. Before granting the requested access, the
        /// system checks the access token of the calling process.
        /// </summary>
        [Flags]
        public enum ServiceAccess : uint
        {
            /// <summary>
            /// Required to call the QueryServiceConfig and
            /// QueryServiceConfig2 functions to query the service configuration.
            /// </summary>
            ServiceQueryConfig = 0x00001,

            /// <summary>
            /// Required to call the ChangeServiceConfig or ChangeServiceConfig2 function
            /// to change the service configuration. Because this grants the caller
            /// the right to change the executable file that the system runs,
            /// it should be granted only to administrators.
            /// </summary>
            ServiceChangeConfig = 0x00002,

            /// <summary>
            /// Required to call the QueryServiceStatusEx function to ask the service
            /// control manager about the status of the service.
            /// </summary>
            ServiceQueryStatus = 0x00004,

            /// <summary>
            /// Required to call the EnumDependentServices function to enumerate all
            /// the services dependent on the service.
            /// </summary>
            ServiceEnumerateDependents = 0x00008,

            /// <summary>
            /// Required to call the StartService function to start the service.
            /// </summary>
            ServiceStart = 0x00010,

            /// <summary>
            ///     Required to call the ControlService function to stop the service.
            /// </summary>
            ServiceStop = 0x00020,

            /// <summary>
            /// Required to call the ControlService function to pause or continue
            /// the service.
            /// </summary>
            ServicePauseContinue = 0x00040,

            /// <summary>
            /// Required to call the EnumDependentServices function to enumerate all
            /// the services dependent on the service.
            /// </summary>
            ServiceInterrogate = 0x00080,

            /// <summary>
            /// Required to call the ControlService function to specify a user-defined
            /// control code.
            /// </summary>
            ServiceUserDefinedControl = 0x00100,

            /// <summary>
            /// Includes STANDARD_RIGHTS_REQUIRED in addition to all access rights in this table.
            /// </summary>
            ServiceAllAccess = (AccessMask.StandardRightsRequired |
                                ServiceQueryConfig |
                                ServiceChangeConfig |
                                ServiceQueryStatus |
                                ServiceEnumerateDependents |
                                ServiceStart |
                                ServiceStop |
                                ServicePauseContinue |
                                ServiceInterrogate |
                                ServiceUserDefinedControl),

            GenericRead = AccessMask.StandardRightsRead |
                          ServiceQueryConfig |
                          ServiceQueryStatus |
                          ServiceInterrogate |
                          ServiceEnumerateDependents,

            GenericWrite = AccessMask.StandardRightsWrite |
                           ServiceChangeConfig,

            GenericExecute = AccessMask.StandardRightsExecute |
                             ServiceStart |
                             ServiceStop |
                             ServicePauseContinue |
                             ServiceUserDefinedControl,

            /// <summary>
            /// Required to call the QueryServiceObjectSecurity or
            /// SetServiceObjectSecurity function to access the SACL. The proper
            /// way to obtain this access is to enable the SE_SECURITY_NAME
            /// privilege in the caller's current access token, open the handle
            /// for ACCESS_SYSTEM_SECURITY access, and then disable the privilege.
            /// </summary>
            AccessSystemSecurity = AccessMask.AccessSystemSecurity,

            /// <summary>
            /// Required to call the DeleteService function to delete the service.
            /// </summary>
            Delete = AccessMask.Delete,

            /// <summary>
            /// Required to call the QueryServiceObjectSecurity function to query
            /// the security descriptor of the service object.
            /// </summary>
            ReadControl = AccessMask.ReadControl,

            /// <summary>
            /// Required to call the SetServiceObjectSecurity function to modify
            /// the Dacl member of the service object's security descriptor.
            /// </summary>
            WriteDac = AccessMask.WriteDac,

            /// <summary>
            /// Required to call the SetServiceObjectSecurity function to modify
            /// the Owner and Group members of the service object's security
            /// descriptor.
            /// </summary>
            WriteOwner = AccessMask.WriteOwner,
        }

        /// <summary>
        /// Service types.
        /// </summary>
        [Flags]
        public enum ServiceType : uint
        {
            /// <summary>
            /// Driver service.
            /// </summary>
            ServiceKernelDriver = 0x00000001,

            /// <summary>
            /// File system driver service.
            /// </summary>
            ServiceFileSystemDriver = 0x00000002,

            /// <summary>
            /// Service that runs in its own process.
            /// </summary>
            ServiceWin32OwnProcess = 0x00000010,

            /// <summary>
            /// Service that shares a process with one or more other services.
            /// </summary>
            ServiceWin32ShareProcess = 0x00000020,

            /// <summary>
            /// The service can interact with the desktop.
            /// </summary>
            ServiceInteractiveProcess = 0x00000100,
        }

        /// <summary>
        /// Service start options
        /// </summary>
        public enum ServiceStart : uint
        {
            /// <summary>
            /// A device driver started by the system loader. This value is valid
            /// only for driver services.
            /// </summary>
            ServiceBootStart = 0x00000000,

            /// <summary>
            /// A device driver started by the IoInitSystem function. This value
            /// is valid only for driver services.
            /// </summary>
            ServiceSystemStart = 0x00000001,

            /// <summary>
            /// A service started automatically by the service control manager
            /// during system startup. For more information, see Automatically
            /// Starting Services.
            /// </summary>
            ServiceAutoStart = 0x00000002,

            /// <summary>
            /// A service started by the service control manager when a process
            /// calls the StartService function. For more information, see
            /// Starting Services on Demand.
            /// </summary>
            ServiceDemandStart = 0x00000003,

            /// <summary>
            /// A service that cannot be started. Attempts to start the service
            /// result in the error code ERROR_SERVICE_DISABLED.
            /// </summary>
            ServiceDisabled = 0x00000004,
        }

        /// <summary>
        /// Severity of the error, and action taken, if this service fails
        /// to start.
        /// </summary>
        public enum ServiceError
        {
            /// <summary>
            /// The startup program ignores the error and continues the startup
            /// operation.
            /// </summary>
            ServiceErrorIgnore = 0x00000000,

            /// <summary>
            /// The startup program logs the error in the event log but continues
            /// the startup operation.
            /// </summary>
            ServiceErrorNormal = 0x00000001,

            /// <summary>
            /// The startup program logs the error in the event log. If the
            /// last-known-good configuration is being started, the startup
            /// operation continues. Otherwise, the system is restarted with
            /// the last-known-good configuration.
            /// </summary>
            ServiceErrorSevere = 0x00000002,

            /// <summary>
            /// The startup program logs the error in the event log, if possible.
            /// If the last-known-good configuration is being started, the startup
            /// operation fails. Otherwise, the system is restarted with the
            /// last-known good configuration.
            /// </summary>
            ServiceErrorCritical = 0x00000003,
        }
    }
}
