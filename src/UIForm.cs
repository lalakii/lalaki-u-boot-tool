using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Management;
using System.Reflection;
using System.Windows.Forms;

namespace lalaki_u_boot_tool.src
{
    class UIForm : Form
    {
        readonly Dictionary<string, string> usbDisk = new();
        ComboBox diskSelect;
        TextBox ubootPathTextBox;
        public UIForm()
        {
            Text = Assembly.GetExecutingAssembly().GetName().Name + ", beta-02";
            Icon = SystemIcons.Application;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Width = 580;
            Height = 242;
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
                    if (deviceId != null && File.Exists(ubootPath) && MessageBox.Show("Continuing will write the specified u-boot to a usb disk, should I continue?", "Ask", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        var ret = Win32API.DDUBoot(8192, File.ReadAllBytes(ubootPath), deviceId);
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
            if (dialog.ShowDialog() == DialogResult.OK && ubootPathTextBox != null)
            {
                ubootPathTextBox.Text = dialog.FileName;
            }
        }
        private void ScanBtn_Click(object sender, EventArgs e)
        {
            using var searcher = new ManagementObjectSearcher("SELECT InterfaceType,Size,Model,DeviceID FROM Win32_DiskDrive");
            diskSelect.Items.Clear();
            using (var col = searcher.Get())
            {
                foreach (var obj in col)
                {
                    using (obj)
                    {
                        string deviceId = string.Empty, model = string.Empty;
                        bool isUsbType = false;
                        double sizeGB = 0;
                        foreach (var info in obj.Properties)
                        {
                            if (info.Name == "InterfaceType")
                            {
                                if (!"USB".Equals(info.Value.ToString(), StringComparison.OrdinalIgnoreCase)) break;
                                isUsbType = true;
                            }
                            else if (info.Name == "Size")
                            {
                                sizeGB = Convert.ToDouble(info.Value) * Math.Pow(2, -30);
                            }
                            else if (info.Name == "Model")
                            {
                                model = info.Value.ToString();
                            }
                            else if (info.Name == "DeviceID")
                            {
                                deviceId = info.Value.ToString();
                            }
                        }
                        if (isUsbType && !string.IsNullOrEmpty(deviceId))
                        {
                            var diskInfo = string.Format("{0}: {1} GB", model, sizeGB.ToString("0.00"));
                            usbDisk[diskInfo] = deviceId;
                            diskSelect.Items.Add(diskInfo);
                        }
                    }
                }
            }
            if (diskSelect.Items.Count > 0) diskSelect.SelectedIndex = 0;
        }
        private void InitView()
        {
            Resize += (_, __) => WindowState = WindowState == FormWindowState.Minimized ? WindowState : FormWindowState.Normal;
            const int padding = 20;
            var diskLabel = new Label() { Text = "USB Disk:", Top = 15, Left = padding };
            var scanBtn = new Button() { Top = diskLabel.Height + diskLabel.Top - 3, Text = "Scan", TextAlign = ContentAlignment.MiddleCenter };
            diskSelect = new ComboBox()
            {
                Left = padding,
                Top = diskLabel.Height + diskLabel.Top,
                Width = Width - scanBtn.Width - (padding * 4),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            scanBtn.Left = diskSelect.Width + (padding * 2);
            scanBtn.Height = diskSelect.Height + 5;
            scanBtn.Click += ScanBtn_Click;
            Controls.Add(diskSelect);
            Controls.Add(diskLabel);
            Controls.Add(scanBtn);
            var ubootLabel = new Label() { Left = padding, Text = "U-Boot:", Top = scanBtn.Top + scanBtn.Height + padding };
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
                Left = padding,
                Width = diskSelect.Width,
                TextAlign = HorizontalAlignment.Left,
                ReadOnly = true
            };
            Controls.Add(ubootPathTextBox);
            selectUBootBtn.Left = (2 * padding) + ubootPathTextBox.Width;
            Controls.Add(selectUBootBtn);
            selectUBootBtn.Click += SelectUBootBtn_Click;
            var ddBtn = new Button
            {
                Text = "DD",
                TextAlign = ContentAlignment.MiddleCenter,
                Top = selectUBootBtn.Top + selectUBootBtn.Height + padding,
                Height = selectUBootBtn.Height + 5,
            };
            ddBtn.Left = ((Width - ddBtn.Width) / 2) - (ddBtn.Width / 4);
            ddBtn.Click += DdBtn_Click;
            Controls.Add(ddBtn);
        }
    }
}