using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ClashSharp.Native
{
    class Shell32
    {
        public const string DllName = "shell32.dll";

        [DllImport(DllName, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr ExtractIcon(IntPtr handle, string fileName, uint iconIndex);

        [DllImport(DllName, SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr handle);

        public static Icon GetShell32Icon(uint iconIndex)
        {
            var iconHandle = ExtractIcon(IntPtr.Zero, DllName, iconIndex);

            return Icon.FromHandle(iconHandle);
        }
    }
}
