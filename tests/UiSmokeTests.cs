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
                form.Show();
                Application.DoEvents();

                FindControl<Label>(form, control => control.Text == "Adds map pins to the marker file ClassicUO's radar loads.");
                FindControl<Label>(form, control => control.Text.Contains("Each entry becomes a TREASURE pin"));
                FindControl<Label>(form, control => control.Text.StartsWith("To see changes in game:"));

                TextBox x = FindControl<TextBox>(form, control => control.AccessibleName == "X coordinate");
                TextBox y = FindControl<TextBox>(form, control => control.AccessibleName == "Y coordinate");
                FindControl<Button>(form, control => control.Text == "Pin this MiB");

                x.Text = "1234";
                x.Select(2, 0);
                MethodInfo handleCoordinateMouseUp = typeof(MainForm).GetMethod("HandleCoordinateMouseUp", BindingFlags.Instance | BindingFlags.NonPublic);
                handleCoordinateMouseUp.Invoke(form, new object[] { x, new MouseEventArgs(MouseButtons.Left, 1, 1, 1, 0) });
                Assert(x.SelectionStart == 0 && x.SelectionLength == x.TextLength, "Clicking a coordinate did not select its full value.");

                x.Text = "4321";
                y.Text = "876";
                MethodInfo handleCoordinateKeyDown = typeof(MainForm).GetMethod("HandleCoordinateKeyDown", BindingFlags.Instance | BindingFlags.NonPublic);
                KeyEventArgs enter = new KeyEventArgs(Keys.Enter);
                handleCoordinateKeyDown.Invoke(form, new object[] { y, enter });
                Assert(enter.Handled && enter.SuppressKeyPress, "Enter was not consumed by the coordinate input.");

                MarkerFileStore saved = new MarkerFileStore(directory);
                Assert(saved.Markers.Count == 1, "Enter did not pin one marker.");
                Assert(saved.Markers[0].X == 4321 && saved.Markers[0].Y == 876, "Enter pinned the wrong coordinates.");
                Label count = FindControl<Label>(form, control => control.Text == "1 chart pinned");
                Assert(count != null, "Pinned count did not refresh.");
                Assert(x.Text == "0" && y.Text == "0", "Successful pin did not reset both coordinates.");
                Assert(x.Focused, "Successful pin did not return focus to X.");
                Assert(x.SelectionStart == 0 && x.SelectionLength == x.TextLength, "Successful pin did not select the next X value.");

                Button elevate = FindControl<Button>(form, control => control.Text == "Restart as administrator");
                Button choose = FindControl<Button>(form, control => control.Text == "Choose folder");
                Button open = FindControl<Button>(form, control => control.Text == "Open folder");
                TextBox path = FindControl<TextBox>(form, control => control.AccessibleName == "Current marker file");
                elevate.Visible = true;
                ScaleFonts(form, 1.25F);
                form.ClientSize = new System.Drawing.Size(1024, 900);
                form.PerformLayout();
                Application.DoEvents();
                AssertButtonTextFits(form);
                AssertSingleLineLabelFits(FindControl<Label>(form, control => control.Text == "1 chart pinned"));
                AssertSingleLineLabelFits(FindControl<Label>(form, control => control.Text.StartsWith("To see changes in game:")));
                AssertLocationLayout(path, choose, open, elevate);
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

    private static void AssertLocationLayout(TextBox path, Button choose, Button open, Button elevate)
    {
        System.Drawing.Rectangle pathBounds = path.RectangleToScreen(path.ClientRectangle);
        System.Drawing.Rectangle chooseBounds = choose.RectangleToScreen(choose.ClientRectangle);
        System.Drawing.Rectangle openBounds = open.RectangleToScreen(open.ClientRectangle);
        System.Drawing.Rectangle elevateBounds = elevate.RectangleToScreen(elevate.ClientRectangle);

        Assert(pathBounds.Bottom <= chooseBounds.Top, "The marker-file path still shares or overlaps the action-button row.");
        Assert(pathBounds.Width >= 700, "The marker-file path loses too much width when permission actions are visible.");
        Assert(!chooseBounds.IntersectsWith(openBounds), "Choose and Open folder buttons overlap.");
        Assert(!openBounds.IntersectsWith(elevateBounds), "Open folder and administrator buttons overlap.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
