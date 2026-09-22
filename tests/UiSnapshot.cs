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
        if (args.Length < 2 || args.Length > 6)
        {
            Console.Error.WriteLine("Usage: UiSnapshot.exe <fixture-folder> <output-png> [font-scale] [show-permission-button] [resize-cycle] [screen-capture]");
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
                form.ClientSize = new Size(1024, 900);
            }

            form.Show();
            Application.DoEvents();
            if (HasArgument(args, "show-permission-button"))
            {
                FindButton(form, "Restart as administrator").Visible = true;
                Label access = FindLabel(form, "MARKER FILE");
                access.Text = "MARKER FILE  •  WINDOWS PERMISSION NEEDED";
                access.ForeColor = Color.FromArgb(168, 57, 48);
                form.PerformLayout();
                Application.DoEvents();
            }
            if (HasArgument(args, "resize-cycle"))
            {
                ExerciseResizeCycle(form, form.ClientSize);
            }
            Thread.Sleep(250);
            Application.DoEvents();

            bool captureScreen = HasArgument(args, "screen-capture");
            if (captureScreen)
            {
                Screen screen = Screen.FromControl(form);
                int captureWidth = screen.Bounds.Width;
                int captureHeight = screen.Bounds.Height;
                form.TopMost = true;
                form.StartPosition = FormStartPosition.Manual;
                form.Size = new Size(
                    Math.Max(form.MinimumSize.Width, Math.Min(form.Width, captureWidth - 40)),
                    Math.Max(form.MinimumSize.Height, Math.Min(form.Height, captureHeight - 40)));
                form.Location = screen.Bounds.Location;
                form.BringToFront();
                form.Activate();
                Application.DoEvents();
                Thread.Sleep(250);
                using (Bitmap bitmap = new Bitmap(captureWidth, captureHeight))
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(screen.Bounds.Location, Point.Empty, screen.Bounds.Size);
                    bitmap.Save(args[1], ImageFormat.Png);
                }
            }
            else
            {
                using (Bitmap bitmap = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
                    bitmap.Save(args[1], ImageFormat.Png);
                }
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

    private static bool HasArgument(string[] args, string expected)
    {
        for (int index = 2; index < args.Length; index++)
        {
            if (String.Equals(args[index], expected, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void ExerciseResizeCycle(Form form, Size finalClientSize)
    {
        Size narrow = new Size(1000, Math.Max(760, finalClientSize.Height - 40));
        Size wide = new Size(1280, finalClientSize.Height + 40);
        for (int index = 0; index < 24; index++)
        {
            form.ClientSize = index % 2 == 0 ? narrow : wide;
            form.PerformLayout();
            Application.DoEvents();
        }

        form.ClientSize = finalClientSize;
        form.PerformLayout();
        form.Refresh();
        Application.DoEvents();
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

    private static Label FindLabel(Control root, string startsWith)
    {
        foreach (Control child in root.Controls)
        {
            Label label = child as Label;
            if (label != null && label.Text.StartsWith(startsWith, StringComparison.Ordinal))
            {
                return label;
            }

            Label nested = FindLabel(child, startsWith);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}

