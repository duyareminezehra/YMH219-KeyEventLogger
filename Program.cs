using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using YMH219_KeyEventLogger.Forms;

namespace YMH219_KeyEventLogger
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();

            if (args.Contains("--generate-screenshots"))
            {
                using var form = new MainForm();
                string screenshotsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "screenshots");
                form.SaveDocumentationScreenshots(Path.GetFullPath(screenshotsDir));
                return;
            }

            Application.Run(new MainForm());
        }
    }
}
