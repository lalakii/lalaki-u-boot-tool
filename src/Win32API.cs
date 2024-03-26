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
        private extern static bool CloseHandle(SafeFileHandle hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private extern static SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll")]
        private extern static bool WriteFile(SafeFileHandle hFile, IntPtr lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, ref NativeOverlapped lpOverlapped);

        public static bool DDUBoot(int seek, byte[] bytes, string deviceId)
        {
            var ret = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (ret)
            {
                var hFile = CreateFile(deviceId, GENERIC_WRITE, 0, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
                ret = !hFile.IsInvalid;
                if (ret)
                {
                    const int buf_len = 4096;
                    int count = bytes.Length / buf_len;
                    int buf_end = bytes.Length % buf_len;
                    for (int i = 0; i <= count; i++)
                    {
                        byte[] buf;
                        if (count == i)
                        {
                            if (buf_end == 0)
                                break;
                            buf = new byte[buf_end];
                        }
                        else
                        {
                            buf = new byte[buf_len];
                        }
                        var indexOffset = i * buf_len;
                        var evt = new ManualResetEvent(false);
                        var lpOverlapped = new NativeOverlapped
                        {
                            OffsetLow = seek + indexOffset,
                            EventHandle = evt.SafeWaitHandle.DangerousGetHandle()
                        };
                        var p_buf = Marshal.AllocHGlobal(buf.Length);
                        Marshal.Copy(bytes, indexOffset, p_buf, buf.Length);
                        ret = WriteFile(hFile, p_buf, buf_len, out _, ref lpOverlapped);
                        evt.WaitOne(5000);
                        Marshal.FreeHGlobal(p_buf);
                        if (!ret) break;
                    }
                }
                CloseHandle(hFile);
            }
            return ret;
        }
    }
}