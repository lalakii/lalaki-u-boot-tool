using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace lalaki_u_boot_tool.src
{/// <summary>
/// 手写的类，没有使用设计器
/// </summary>
    internal partial class UiForm : Form
    {
        private ComboBox diskSelect;
        private TextBox ubootPathTextBox;
        protected override Size DefaultSize => new(670, 318);
        protected override Padding DefaultPadding => new(20);

        private readonly OpenFileDialog dialog = new()
        {
            Filter = "Binary Files|*.bin|All Files|*.*",
            CheckFileExists = true
        };

        public UiForm()
        {
            InitView();
        }

        /// <summary>
        /// 不维护的代码，随便写的
        /// </summary>
        private void InitView()
        {
            Font defaultFont = new Font("Arial", 14);
            Icon = SystemIcons.Application;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            StartPosition = FormStartPosition.CenterScreen;
            Assembly assembly = Assembly.GetExecutingAssembly();
            AssemblyName main = assembly.GetName();
            Text = string.Format("{0} - {1}", main.Name, main.Version);
            Resize += (_, __) => WindowState = WindowState == FormWindowState.Minimized ? WindowState : FormWindowState.Normal;
            Label diskLabel = new Label() { Width = Width, Text = "USB Drive:", Top = Padding.Top, Left = Padding.Left, Font = defaultFont };
            int scanBtnTop = diskLabel.Height + (diskLabel.Top * 2);
            Button scanBtn = new Button
            {
                Top = scanBtnTop,
                BackgroundImageLayout = ImageLayout.Center,
                BackgroundImage = new Bitmap(assembly.GetManifestResourceStream("lalaki_u_boot_tool.icons.scan.png")),
                TextAlign = ContentAlignment.MiddleCenter,
                Width = 50
            };
            diskSelect = new ComboBox()
            {
                Left = Padding.Left,
                Top = scanBtnTop,
                Width = Width - scanBtn.Width - (Padding.Left * 4),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = defaultFont
            };
            scanBtn.Left = diskSelect.Width + (Padding.Left * 2);
            scanBtn.Height = diskSelect.Height + 12;
            scanBtn.Click += ScanBtn_Click;
            Controls.Add(diskSelect);
            Controls.Add(diskLabel);
            Controls.Add(scanBtn);
            Label ubootLabel = new Label() { Width = Width, Font = defaultFont, Left = Padding.Left, Text = "U-Boot:", Top = scanBtn.Top + scanBtn.Height + Padding.Left };
            Controls.Add(ubootLabel);
            Button selectUBootBtn = new Button()
            {
                Top = ubootLabel.Top + ubootLabel.Height + Padding.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = scanBtn.Height,
                Width = scanBtn.Width,
                BackgroundImageLayout = ImageLayout.Center,
                BackgroundImage = new Bitmap(assembly.GetManifestResourceStream("lalaki_u_boot_tool.icons.open_file.png"))
            };
            ubootPathTextBox = new TextBox()
            {
                Top = ubootLabel.Top + ubootLabel.Height + Padding.Top,
                Left = Padding.Left,
                Width = diskSelect.Width,
                TextAlign = HorizontalAlignment.Left,
                ReadOnly = true,
                Font = defaultFont
            };
            Controls.Add(ubootPathTextBox);
            selectUBootBtn.Left = (2 * Padding.Left) + ubootPathTextBox.Width;
            Controls.Add(selectUBootBtn);
            selectUBootBtn.Click += SelectUBootBtn_Click;
            Button ddBtn = new Button
            {
                BackgroundImage = new Bitmap(assembly.GetManifestResourceStream("lalaki_u_boot_tool.icons.flash.png")),
                TextAlign = ContentAlignment.MiddleCenter,
                Top = selectUBootBtn.Top + selectUBootBtn.Height + Padding.Left,
                BackgroundImageLayout = ImageLayout.Center,
                Height = selectUBootBtn.Height + 8,
            };
            ddBtn.Click += DdBtn_Click;
            Controls.Add(ddBtn);
            ddBtn.Left = (Width - ddBtn.Width) / 2;
        }
    }
}