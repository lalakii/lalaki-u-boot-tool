using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace lalaki_u_boot_tool.src
{/// <summary>
/// 向磁盘写入数据的类 by lalaki.cn
/// </summary>
    public static class Win32Api
    {
        private const uint OPEN_EXISTING = 3;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(SafeFileHandle hObject);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll")]
        private static extern bool WriteFile(SafeFileHandle hFile, IntPtr lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, ref NativeOverlapped lpOverlapped);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

        [DllImport("kernel32.dll")]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

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
                        for (var j = indexOffset; j < indexOffset + buf.Length; j++)
                        {
                            buf[j - indexOffset] = bytes[j];
                        }
                        var evt = CreateEvent(IntPtr.Zero, false, false, null);
                        var lpOverlapped = new NativeOverlapped
                        {
                            OffsetLow = seek + indexOffset,
                            EventHandle = evt
                        };
                        var p_buf = Marshal.UnsafeAddrOfPinnedArrayElement(buf, 0);
                        ret = WriteFile(hFile, p_buf, buf_len, out _, ref lpOverlapped);
                        WaitForSingleObject(evt, 3000);
                        CloseHandle(evt);
                        if (!ret) break;
                    }
                }
                CloseHandle(hFile);
            }
            return ret;
        }
    }
}