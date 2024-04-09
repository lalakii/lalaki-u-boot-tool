using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;

namespace lalaki_u_boot_tool.src
{/// <summary>
 /// 查询USB存储设备，以及向磁盘写入数据的类 by lalaki.cn
 /// </summary>
    internal static class Win32Api
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

        /// <summary>
        /// 写入数据到磁盘的方法
        /// </summary>
        /// <param name="seek">起始位置</param>
        /// <param name="bytes">需要写入的数据</param>
        /// <param name="deviceId">磁盘的原始Id，注意不是盘符</param>
        /// <returns>返回true即写入成功</returns>
        internal static bool WriteToDrive(int seek, byte[] bytes, string deviceId)
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
                    int buf_end = bytes.Length & (buf_len - 1);//当buf_len是2的n次方时才有效，否则使用模运算取余数
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
                        ret = WriteFile(hFile, Marshal.UnsafeAddrOfPinnedArrayElement(buf, 0), buf_len, out _, ref lpOverlapped);
                        WaitForSingleObject(evt, 3000);
                        CloseHandle(evt);
                        if (!ret) break;
                    }
                }
                CloseHandle(hFile);
            }
            return ret;
        }

        /// <summary>
        /// 列出磁盘上的USB存储设备，传递一个不为空的map，用于保存数据
        /// </summary>
        /// <param name="drive">key是磁盘的名称及容量，value是磁盘的deviceId</param>
        internal static void EnumUSBDrives(Dictionary<string, string> drive)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var objects = new ManagementObjectSearcher("SELECT DeviceID,Model,Size FROM Win32_DiskDrive WHERE InterfaceType='USB'");
                using var col = objects.Get();
                foreach (var obj in col)
                {
                    object model = "";
                    string deviceId = null;
                    var sizeGB = 0.0;
                    using (obj)
                    {
                        foreach (var info in obj.Properties)
                        {
                            switch (info.Name)
                            {
                                case "DeviceID":
                                    deviceId = info.Value.ToString();
                                    break;

                                case "Model":
                                    model = info.Value;
                                    break;

                                case "Size":
                                    sizeGB = Convert.ToUInt64(info.Value) / 1073741824.0;
                                    break;
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(deviceId))
                        continue;
                    drive[string.Format("{0}: {1:0.00} GB", model, sizeGB)] = deviceId;
                }
            }
        }
    }
}