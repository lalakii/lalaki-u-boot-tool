using System;
using System.Windows.Forms;
using System.Threading;
namespace lalaki_u_boot_tool.src
{
    static class Launch
    {
        readonly static bool createdNew;
        readonly static Mutex _mutex = new(true, typeof(Launch).Namespace, out createdNew);
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (createdNew)
            {
                Application.Run(new UIForm());
            }
            else
            {
                using (_mutex)
                    MessageBox.Show("The program is already running!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Environment.FailFast("");
            }
        }
    }
}