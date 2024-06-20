using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace lalaki_u_boot_tool.src
{
    internal partial class UiForm : Form
    {
        private readonly Dictionary<string, string> usbDrive = [];

        private void DdBtn_Click(object sender, EventArgs e)
        {
            string ubootPath = ubootPathTextBox.Text;
            if (File.Exists(ubootPath) && usbDrive.TryGetValue(diskSelect.Text, out string deviceId) && MessageBox.Show("Continuing will write the specified u-boot to a usb disk, should I continue?", "Ask", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                bool ret = Win32Api.WriteToDrive(8192, ubootPath, deviceId);
                MessageBox.Show("Reporting error: " + (ret ? 0 : 1), "Message", MessageBoxButtons.OK, ret ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            }
        }

        private void SelectUBootBtn_Click(object sender, EventArgs e)
        {
            if (dialog.ShowDialog() == DialogResult.OK)
                ubootPathTextBox.Text = dialog.FileName;
        }

        private void ScanBtn_Click(object sender, EventArgs e)
        {
            diskSelect.Items.Clear();
            Win32Api.EnumUSBDrives(usbDrive);
            foreach (KeyValuePair<string, string> drive in usbDrive)
                diskSelect.Items.Add(drive.Key);
            if (diskSelect.Items.Count > 0)
                diskSelect.SelectedIndex = 0;
        }
    }
}