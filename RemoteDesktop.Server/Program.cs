using System;
using System.Windows.Forms;
using RemoteDesktop.Server.UI;

namespace RemoteDesktop.Server
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration_Initialize();
            Application.Run(new ServerMainForm());
        }

        private static void ApplicationConfiguration_Initialize()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
        }
    }
}
