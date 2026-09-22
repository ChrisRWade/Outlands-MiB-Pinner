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

                Button alwaysOnTop = FindControl<Button>(form, control => control.AccessibleName == "Keep window on top");
                Assert(!form.TopMost && alwaysOnTop.Text == "Keep on top", "Keep on top did not start in its safe off state.");
                alwaysOnTop.PerformClick();
                Application.DoEvents();
                Assert(form.TopMost && alwaysOnTop.Text == "Pinned on top", "Keep on top did not enable the form's TopMost state.");
                Assert(alwaysOnTop.AccessibleDescription.Contains("pinned above"), "Keep on top did not expose its active state to accessibility tools.");

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
                ExerciseResizeCycle(form, new System.Drawing.Size(1024, 900));
                AssertBufferedLayoutContainers(form);
                AssertButtonTextFits(form);
                AssertSingleLineLabelFits(FindControl<Label>(form, control => control.Text == "1 chart pinned"));
                AssertSingleLineLabelFits(FindControl<Label>(form, control => control.Text.StartsWith("To see changes in game:")));
                AssertLocationLayout(path, choose, open, elevate);

                FieldInfo storeField = typeof(MainForm).GetField("_store", BindingFlags.Instance | BindingFlags.NonPublic);
                MarkerFileStore liveStore = (MarkerFileStore)storeField.GetValue(form);
                MapMarker second = liveStore.Add(111, 222);
                liveStore.Add(333, 444);
                MapMarker fourth = liveStore.Add(555, 666);
                liveStore.Remove(new[] { fourth, second });

                MethodInfo populateGrid = typeof(MainForm).GetMethod("PopulateGrid", BindingFlags.Instance | BindingFlags.NonPublic);
                populateGrid.Invoke(form, null);
                Application.DoEvents();

                DataGridView grid = FindControl<DataGridView>(form, control => control.AccessibleName == "Pinned bottle maps");
                Button remove = FindControl<Button>(form, control => control.Text == "Mark completed");
                Button renumber = FindControl<Button>(form, control => control.Text == "Renumber");
                Assert(grid.Rows.Count == 2, "Expected two remaining rows after arbitrary-order removal.");
                Assert(grid.SelectedRows.Count == 0 && !remove.Enabled, "Grid rebuild left a phantom selected row with inconsistent delete state.");
                Assert(renumber.Enabled, "Renumber should be available when MiB labels contain a gap.");

                foreach (DataGridViewRow row in grid.Rows)
                {
                    grid.ClearSelection();
                    grid.CurrentCell = row.Cells[0];
                    row.Selected = true;
                    Application.DoEvents();
                    Assert(remove.Enabled, "A remaining row could not enable Mark completed after grid rebuild.");
                }

                liveStore.RenumberSequentially();
                populateGrid.Invoke(form, null);
                Application.DoEvents();
                Assert((string)grid.Rows[0].Cells[0].Value == "MiB 001" && (string)grid.Rows[1].Cells[0].Value == "MiB 002", "Renumbered labels were not reflected in the grid.");
                Assert(!renumber.Enabled, "Renumber stayed enabled after labels became consecutive.");

                alwaysOnTop.PerformClick();
                Application.DoEvents();
                Assert(!form.TopMost && alwaysOnTop.Text == "Keep on top", "Keep on top did not return the form to its normal window state.");
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

    private static void ExerciseResizeCycle(Form form, System.Drawing.Size finalClientSize)
    {
        System.Drawing.Size narrow = new System.Drawing.Size(1000, Math.Max(820, finalClientSize.Height - 40));
        System.Drawing.Size wide = new System.Drawing.Size(1280, finalClientSize.Height + 40);
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

    private static void AssertBufferedLayoutContainers(Control root)
    {
        Queue<Control> pending = new Queue<Control>();
        pending.Enqueue(root);
        while (pending.Count > 0)
        {
            Control current = pending.Dequeue();
            TableLayoutPanel table = current as TableLayoutPanel;
            if (table != null)
            {
                Assert(table is BufferedTableLayoutPanel, "An unbuffered table layout remains in the resize paint path.");
                Assert(table.BackColor.A == 255, "A transparent table layout can leave stale pixels during resize.");
            }
            else if (current is Panel)
            {
                Assert(current is BufferedPanel, "An unbuffered panel remains in the resize paint path.");
            }

            foreach (Control child in current.Controls)
            {
                pending.Enqueue(child);
            }
        }
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
