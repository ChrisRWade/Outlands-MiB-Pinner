using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using WadesMiBPinner;

internal static class UiSnapshot
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length < 2 || args.Length > 4)
        {
            Console.Error.WriteLine("Usage: UiSnapshot.exe <fixture-folder> <output-png> [font-scale] [show-permission-button]");
            return 2;
        }

        float fontScale = args.Length >= 3
            ? Single.Parse(args[2], CultureInfo.InvariantCulture)
            : 1F;

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
            if (fontScale > 1F)
            {
                ScaleFonts(form, fontScale);
                form.ClientSize = new Size(1024, 850);
            }

            form.Show();
            Application.DoEvents();
            if (args.Length == 4 && String.Equals(args[3], "show-permission-button", StringComparison.OrdinalIgnoreCase))
            {
                FindButton(form, "Restart as administrator").Visible = true;
                form.PerformLayout();
                Application.DoEvents();
            }
            Thread.Sleep(250);
            Application.DoEvents();

            using (Bitmap bitmap = new Bitmap(form.Width, form.Height))
            {
                form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
                bitmap.Save(args[1], ImageFormat.Png);
            }

            form.Close();
        }

        return 0;
    }

    private static void ScaleFonts(Control root, float scale)
    {
        foreach (Control child in root.Controls)
        {
            ScaleFonts(child, scale);
        }
        root.Font = new Font(root.Font.FontFamily, root.Font.Size * scale, root.Font.Style, root.Font.Unit);
    }

    private static Button FindButton(Control root, string text)
    {
        foreach (Control child in root.Controls)
        {
            Button button = child as Button;
            if (button != null && button.Text == text)
            {
                return button;
            }

            Button nested = FindButton(child, text);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}

