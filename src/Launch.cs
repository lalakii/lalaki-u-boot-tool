using System.Collections.Generic;
using System.Drawing;
using System;
using System.Management;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Reflection;

namespace lalaki_u_boot_tool.src
{
    static class Launch
    {
        static bool createNew;
        readonly static Mutex _mutex = new Mutex(true, typeof(Launch).Namespace, out createNew);
        [STAThread]
        static void Main()
        {
            if (!createNew)
            {
                using (_mutex)
                {
                    MessageBox.Show("The program is already running!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Environment.FailFast("");
                }
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UIForm());
        }
        class UIForm : Form
        {
            readonly Dictionary<string, string> usbDisk = new Dictionary<string, string>();
            ComboBox diskSelect;
            TextBox ubootPathTextBox;
            public UIForm()
            {
                Text = Assembly.GetExecutingAssembly().GetName().Name + ", beta-01";
                Icon = SystemIcons.Application;
                FormBorderStyle = FormBorderStyle.FixedSingle;
                Width = 550;
                Height = 242;
                MaximizeBox = false;
                StartPosition = FormStartPosition.CenterScreen;
                InitView();
            }
            public void InitView()
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
            private void DdBtn_Click(object sender, EventArgs e)
            {
                if (sender is Button btn)
                {
                    btn.Enabled = false;
                    var diskKey = diskSelect.Text;
                    if (diskKey != null && usbDisk.TryGetValue(diskKey, out string value))
                    {
                        var ubootPath = ubootPathTextBox.Text;
                        if (value != null && File.Exists(ubootPath) && MessageBox.Show("Continuing will write the specified u-boot to a usb disk, should I continue?", "Ask", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            var lastWin32Error = Win32API.DDUBoot(8192, File.ReadAllBytes(ubootPath), value);
                            MessageBox.Show("Reporting error: " + (lastWin32Error ? 1 : 0), "Message", MessageBoxButtons.OK, lastWin32Error ? MessageBoxIcon.Error : MessageBoxIcon.Information);
                        }
                    }
                    btn.Enabled = true;
                }
            }
            private void SelectUBootBtn_Click(object sender, EventArgs e)
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Binary Files|*.bin|All Files|*.*"
                };
                if (dialog.ShowDialog() == DialogResult.OK && ubootPathTextBox != null)
                {
                    ubootPathTextBox.Text = dialog.FileName;
                }
            }
            private void ScanBtn_Click(object sender, EventArgs e)
            {
                var searcher = new ManagementObjectSearcher("SELECT InterfaceType,Size,Model,DeviceID FROM Win32_DiskDrive");
                diskSelect.Items.Clear();
                foreach (var disk in searcher.Get())
                {
                    bool isUsbType = false;
                    string deviceId = string.Empty, model = string.Empty;
                    double sizeGB = 0;
                    foreach (var info in disk.Properties)
                    {
                        if (info.Name == "InterfaceType")
                        {
                            if (info.Value.ToString().Trim() != "USB") break;
                            isUsbType = true;
                        }
                        else if (info.Name == "Size")
                        {
                            sizeGB = Convert.ToDouble(info.Value) / (1024 * 1024 * 1024);
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
                if (diskSelect.Items.Count > 0) diskSelect.SelectedIndex = 0;
            }
        }
    }
}