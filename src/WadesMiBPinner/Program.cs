using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace WadesMiBPinner
{
    internal static class Program
    {
        public const string DefaultMarkerDirectory = @"C:\Program Files (x86)\Ultima Online Outlands\ClassicUO\Data\Client";

        [STAThread]
        private static void Main(string[] args)
        {
            bool ownsMutex;
            using (Mutex mutex = new Mutex(true, @"Local\WadesMiBPinner-8C4C1526", out ownsMutex))
            {
                if (!ownsMutex)
                {
                    MessageBox.Show(
                        "Wade's MiB Pinner is already open.",
                        "Already running",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                string directory = ReadFolderArgument(args);
                if (String.IsNullOrWhiteSpace(directory))
                {
                    directory = DefaultMarkerDirectory;
                }

                Application.Run(new MainForm(directory));
                GC.KeepAlive(mutex);
            }
        }

        private static string ReadFolderArgument(string[] args)
        {
            if (args == null)
            {
                return null;
            }

            for (int index = 0; index < args.Length - 1; index++)
            {
                if (String.Equals(args[index], "--folder", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        return Path.GetFullPath(args[index + 1]);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }

            return null;
        }
    }
}

