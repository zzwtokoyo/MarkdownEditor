using System;
using System.Windows.Forms;

namespace MarkdownEditor
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // 启用高DPI支持
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            Application.Run(new MainForm());
        }
    }
}