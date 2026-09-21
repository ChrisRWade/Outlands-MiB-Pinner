using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using WadesMiBPinner;

internal static class UiSmokeTests
{
    [STAThread]
    private static int Main()
    {
        string directory = Path.Combine(Path.GetTempPath(), "WadesMiBPinnerUiTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (MainForm form = new MainForm(directory))
            {
                MethodInfo loadDirectory = typeof(MainForm).GetMethod("LoadDirectory", BindingFlags.Instance | BindingFlags.NonPublic);
                loadDirectory.Invoke(form, new object[] { directory, false });

                FindControl<Label>(form, control => control.Text == "Adds map pins to the marker file ClassicUO's radar loads.");
                FindControl<Label>(form, control => control.Text.Contains("Each entry becomes a TREASURE pin"));
                FindControl<Label>(form, control => control.Text.StartsWith("To see changes in game:"));

                NumericUpDown x = FindControl<NumericUpDown>(form, control => control.AccessibleName == "X coordinate");
                NumericUpDown y = FindControl<NumericUpDown>(form, control => control.AccessibleName == "Y coordinate");
                FindControl<Button>(form, control => control.Text == "Pin this MiB");
                x.Value = 4321;
                y.Value = 876;
                MethodInfo handleAdd = typeof(MainForm).GetMethod("HandleAdd", BindingFlags.Instance | BindingFlags.NonPublic);
                handleAdd.Invoke(form, new object[] { form, EventArgs.Empty });

                MarkerFileStore saved = new MarkerFileStore(directory);
                Assert(saved.Markers.Count == 1, "Pin button did not save one marker.");
                Assert(saved.Markers[0].X == 4321 && saved.Markers[0].Y == 876, "Pin button saved the wrong coordinates.");
                Label count = FindControl<Label>(form, control => control.Text == "1 chart pinned");
                Assert(count != null, "Pinned count did not refresh.");

                Button elevate = FindControl<Button>(form, control => control.Text == "Restart as administrator");
                elevate.Visible = true;
                ScaleFonts(form, 1.25F);
                form.ClientSize = new System.Drawing.Size(1024, 850);
                form.PerformLayout();
                AssertButtonTextFits(form);
                AssertSingleLineLabelFits(FindControl<Label>(form, control => control.Text == "1 chart pinned"));
                AssertSingleLineLabelFits(FindControl<Label>(form, control => control.Text.StartsWith("To see changes in game:")));
            }

            Console.WriteLine("PASS  loads the form and pins coordinates through the real add handler");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static T FindControl<T>(Control root, Predicate<T> matches) where T : Control
    {
        Queue<Control> pending = new Queue<Control>();
        pending.Enqueue(root);
        while (pending.Count > 0)
        {
            Control current = pending.Dequeue();
            T typed = current as T;
            if (typed != null && matches(typed))
            {
                return typed;
            }

            foreach (Control child in current.Controls)
            {
                pending.Enqueue(child);
            }
        }

        throw new InvalidOperationException("Expected control was not found.");
    }

    private static void ScaleFonts(Control root, float scale)
    {
        foreach (Control child in root.Controls)
        {
            ScaleFonts(child, scale);
        }
        root.Font = new System.Drawing.Font(root.Font.FontFamily, root.Font.Size * scale, root.Font.Style, root.Font.Unit);
    }

    private static void AssertButtonTextFits(Control root)
    {
        Queue<Control> pending = new Queue<Control>();
        pending.Enqueue(root);
        while (pending.Count > 0)
        {
            Control current = pending.Dequeue();
            Button button = current as Button;
            if (button != null)
            {
                System.Drawing.Size measured = TextRenderer.MeasureText(
                    button.Text,
                    button.Font,
                    new System.Drawing.Size(Int32.MaxValue, Int32.MaxValue),
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                int requiredWidth = measured.Width + button.Padding.Horizontal + 8;
                int requiredHeight = measured.Height + button.Padding.Vertical + 8;
                Assert(button.ClientSize.Width >= requiredWidth, button.Text + " button text is horizontally clipped.");
                Assert(button.ClientSize.Height >= requiredHeight, button.Text + " button text is vertically clipped.");
            }

            foreach (Control child in current.Controls)
            {
                pending.Enqueue(child);
            }
        }
    }

    private static void AssertSingleLineLabelFits(Label label)
    {
        System.Drawing.Size measured = TextRenderer.MeasureText(
            label.Text,
            label.Font,
            new System.Drawing.Size(Int32.MaxValue, Int32.MaxValue),
            TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        Assert(label.ClientSize.Width >= measured.Width, label.Text + " label is horizontally clipped.");
        Assert(label.ClientSize.Height >= measured.Height, label.Text + " label is vertically clipped.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
