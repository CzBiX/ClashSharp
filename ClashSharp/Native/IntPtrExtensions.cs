using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ClashSharp.Native
{
    static class IntPtrExtensions
    {
        public static SafeHandle AsSafeHandle(this IntPtr ptr, Func<IntPtr, bool> closeHandle)
        {
            return new SafeCloseHandle(ptr, closeHandle);
        }
    }

    class SafeCloseHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private readonly Func<IntPtr, bool> closeHandle;

        public SafeCloseHandle(IntPtr handle, Func<IntPtr, bool> closeHandle) : base(true)
        {
            this.handle = handle;
            this.closeHandle = closeHandle;
        }

        protected override bool ReleaseHandle() => closeHandle(handle);
    }
}
