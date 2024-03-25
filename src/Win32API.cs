using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Threading;
namespace lalaki_u_boot_tool.src
{
    public static class Win32API
    {
        private const uint OPEN_EXISTING = 3;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private enum EMoveMethod : uint
        {
            Begin = 0,
            Current = 1,
            End = 2
        }
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private extern static bool CloseHandle(SafeFileHandle hObject);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private extern static SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, [Optional] IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private unsafe extern static bool WriteFile(SafeFileHandle hFile, byte* lpBuffer, uint nNumberOfBytesToWrite, out int lpNumberOfBytesWritten, [In] ref NativeOverlapped lpOverlapped);
        public static bool DDUBoot(int seek, byte[] bytes, string diskPath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                bool lastWin32Error = false;
                var diskHandle = CreateFile(diskPath, GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
                lastWin32Error = Marshal.GetLastWin32Error() != 0;
                if (!lastWin32Error)
                {
                    const int buf_len = 4096;
                    long count = bytes.Length / buf_len;
                    long end = bytes.Length % buf_len;
                    for (int i = 0; i <= count; i++)
                    {
                        byte[] buf;
                        if (i != count)
                        {
                            buf = new byte[buf_len];
                        }
                        else
                        {
                            if (end == 0)
                                break;
                            buf = new byte[end];
                        }
                        Array.Copy(bytes, i * buf_len, buf, 0, buf.Length);
                        var evt = new ManualResetEvent(false);
                        var lpOverlapped = new NativeOverlapped
                        {
                            OffsetLow = (i * buf_len) + seek,
                            EventHandle = evt.SafeWaitHandle.DangerousGetHandle()
                        };
                        unsafe
                        {
                            fixed (byte* p_buf = buf)
                                WriteFile(diskHandle, p_buf, buf_len, out int len, ref lpOverlapped);
                        }
                        evt.WaitOne(5000);
                        lastWin32Error = Marshal.GetLastWin32Error() != 0;
                        if (lastWin32Error) break;
                    }
                }
                CloseHandle(diskHandle);
                return lastWin32Error;
            }
            return false;
        }
    }
}