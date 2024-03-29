using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Management;
using System.Reflection;
using System.Windows.Forms;

namespace lalaki_u_boot_tool.src
{
    internal class UIForm : Form
    {
        protected override Size DefaultSize => new(580, 248);
        protected override Padding DefaultPadding => new(20);
        private readonly Dictionary<string, string> usbDisk = [];
        private ComboBox diskSelect;
        private TextBox ubootPathTextBox;

        public UIForm()
        {
            var main = Assembly.GetExecutingAssembly().GetName();
            Text = main.Name + " - " + main.Version;
            Icon = SystemIcons.Application;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            InitView();
        }

        private void DdBtn_Click(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                btn.Enabled = false;
                var diskInfo = diskSelect.Text;
                if (diskInfo != null && usbDisk.TryGetValue(diskInfo, out string deviceId))
                {
                    var ubootPath = ubootPathTextBox.Text;
                    if (File.Exists(ubootPath) && MessageBox.Show("Continuing will write the specified u-boot to a usb disk, should I continue?", "Ask", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        var ret = Win32Api.DDUBoot(8192, File.ReadAllBytes(ubootPath), deviceId);
                        MessageBox.Show("Reporting error: " + (ret ? 0 : 1), "Message", MessageBoxButtons.OK, ret ? MessageBoxIcon.Information : MessageBoxIcon.Error);
                    }
                }
                btn.Enabled = true;
            }
        }

        private void SelectUBootBtn_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Binary Files|*.bin|All Files|*.*",
                CheckFileExists = true
            };
            if (dialog.ShowDialog() == DialogResult.OK)
                ubootPathTextBox.Text = dialog.FileName;
        }

        private void ScanBtn_Click(object sender, EventArgs e)
        {
            diskSelect.Items.Clear();
            using var searcher = new ManagementObjectSearcher("SELECT DeviceID,Model,Size FROM Win32_DiskDrive WHERE InterfaceType='USB'");
            using var col = searcher.Get();
            foreach (var obj in col)
            {
                using (obj)
                {
                    string deviceId = string.Empty, model = string.Empty;
                    var sizeGB = 0.0;
                    foreach (var info in obj.Properties)
                    {
                        if (info.Name == "DeviceID")
                        {
                            deviceId = info.Value.ToString();
                        }
                        else if (info.Name == "Model")
                        {
                            model = info.Value.ToString();
                        }
                        else if (info.Name == "Size")
                        {
                            sizeGB = Convert.ToUInt64(info.Value) * Math.Pow(2, -30);
                        }
                    }
                    if (string.IsNullOrEmpty(deviceId)) continue;
                    var diskInfo = string.Format("{0}: {1:0.00} GB", model, sizeGB);
                    usbDisk[diskInfo] = deviceId;
                    diskSelect.Items.Add(diskInfo);
                }
            }
            if (diskSelect.Items.Count > 0) diskSelect.SelectedIndex = 0;
        }

        private void InitView()
        {
            Resize += (_, __) => WindowState = WindowState == FormWindowState.Minimized ? WindowState : FormWindowState.Normal;
            var diskLabel = new Label() { Text = "USB Disk:", Top = Padding.Top, Left = Padding.Left };
            var scanBtn = new Button() { Top = diskLabel.Height + diskLabel.Top - 3, Text = "Scan", TextAlign = ContentAlignment.MiddleCenter };
            diskSelect = new ComboBox()
            {
                Left = Padding.Left,
                Top = diskLabel.Height + diskLabel.Top,
                Width = Width - scanBtn.Width - (Padding.Left * 4),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            scanBtn.Left = diskSelect.Width + (Padding.Left * 2);
            scanBtn.Height = diskSelect.Height + 5;
            scanBtn.Click += ScanBtn_Click;
            Controls.Add(diskSelect);
            Controls.Add(diskLabel);
            Controls.Add(scanBtn);
            var ubootLabel = new Label() { Left = Padding.Left, Text = "U-Boot:", Top = scanBtn.Top + scanBtn.Height + Padding.Left };
            Controls.Add(ubootLabel);
            var selectUBootBtn = new Button()
            {
                Top = ubootLabel.Top + ubootLabel.Height,
                Text = "...",
                TextAlign = ContentAlignment.MiddleCenter,
                Height = diskSelect.Height + 5,
            };
            ubootPathTextBox = new TextBox()
            {
                Top = selectUBootBtn.Top + 2,
                Left = Padding.Left,
                Width = diskSelect.Width,
                TextAlign = HorizontalAlignment.Left,
                ReadOnly = true
            };
            Controls.Add(ubootPathTextBox);
            selectUBootBtn.Left = (2 * Padding.Left) + ubootPathTextBox.Width;
            Controls.Add(selectUBootBtn);
            selectUBootBtn.Click += SelectUBootBtn_Click;
            var ddBtn = new Button
            {
                Text = "DD",
                TextAlign = ContentAlignment.MiddleCenter,
                Top = selectUBootBtn.Top + selectUBootBtn.Height + Padding.Left,
                Height = selectUBootBtn.Height + 5,
            };
            ddBtn.Click += DdBtn_Click;
            Controls.Add(ddBtn);
            ddBtn.Left = (Width - ddBtn.Width) / 2;
        }
    }
}