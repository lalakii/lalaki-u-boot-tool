using System;
using System.Threading;
using System.Windows.Forms;

namespace lalaki_u_boot_tool.src
{
    internal static class App
    {
        private static readonly bool createdNew;
        private static readonly Mutex _mutex = new(true, typeof(App).Namespace, out createdNew);

        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (!createdNew)
            {
                using (_mutex)
                    MessageBox.Show("The program is already running!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Environment.FailFast("");
            }
            Application.Run(new UiForm());
        }
    }
}