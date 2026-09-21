using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using WadesMiBPinner;

internal static class UiSnapshot
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: UiSnapshot.exe <fixture-folder> <output-png>");
            return 2;
        }

        Directory.CreateDirectory(args[0]);
        MarkerFileStore store = new MarkerFileStore(args[0]);
        if (store.Markers.Count == 0)
        {
            store.Add(5450, 502);
            store.Add(6022, 1877);
            store.Add(7314, 294);
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using (MainForm form = new MainForm(args[0]))
        {
            form.Show();
            Application.DoEvents();
            Thread.Sleep(250);
            Application.DoEvents();

            using (Bitmap bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height))
            {
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.ClientSize));
                bitmap.Save(args[1], ImageFormat.Png);
            }

            form.Close();
        }

        return 0;
    }
}

