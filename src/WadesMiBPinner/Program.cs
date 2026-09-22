using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace WadesMiBPinner
{
    internal static class Program
    {
        private const string SingleInstanceMutexName = @"Local\WadesMiBPinner-8C4C1526";
        private const string WaitForExistingInstanceArgument = "--wait-for-existing-instance";
        private const int RestartHandoffTimeoutMilliseconds = 10000;

        public const string DefaultMarkerDirectory = @"C:\Program Files (x86)\Ultima Online Outlands\ClassicUO\Data\Client";

        [STAThread]
        private static void Main(string[] args)
        {
            bool ownsMutex = false;
            bool waitForExistingInstance = ShouldWaitForExistingInstance(args);
            using (Mutex mutex = new Mutex(false, SingleInstanceMutexName))
            {
                try
                {
                    ownsMutex = TryAcquireSingleInstance(
                        mutex,
                        waitForExistingInstance,
                        RestartHandoffTimeoutMilliseconds);
                    if (!ownsMutex)
                    {
                        MessageBox.Show(
                            waitForExistingInstance
                                ? "The previous Wade's MiB Pinner window did not close in time. Close it and try Restart as administrator again."
                                : "Wade's MiB Pinner is already open.",
                            waitForExistingInstance ? "Restart did not complete" : "Already running",
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
                }
                finally
                {
                    if (ownsMutex)
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }
        }

        internal static bool ShouldWaitForExistingInstance(string[] args)
        {
            if (args == null)
            {
                return false;
            }

            foreach (string argument in args)
            {
                if (String.Equals(argument, WaitForExistingInstanceArgument, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool TryAcquireSingleInstance(Mutex mutex, bool waitForExistingInstance, int waitMilliseconds)
        {
            try
            {
                return mutex.WaitOne(waitForExistingInstance ? waitMilliseconds : 0);
            }
            catch (AbandonedMutexException)
            {
                return true;
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

