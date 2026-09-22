using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WadesMiBPinner
{
    public sealed partial class MainForm
    {
        private bool _directoryWritable;

        private void HandleShown(object sender, EventArgs e)
        {
            LoadDirectory(_initialDirectory, false);
            FocusAndSelectCoordinate(_xInput);
        }

        private void LoadDirectory(string directoryPath, bool selectedByUser)
        {
            if (String.IsNullOrWhiteSpace(directoryPath))
            {
                return;
            }

            try
            {
                _store = new MarkerFileStore(directoryPath);
                _pathTextBox.Text = _store.FilePath;
                UpdateAccessState();
                PopulateGrid();
                SetStatus(_store.FileExists
                    ? "Loaded " + _store.Markers.Count + " pinned chart" + PluralSuffix(_store.Markers.Count) + " from the radar marker file."
                    : "Ready to create " + MarkerFileStore.MarkerFileName + " when you add the first marker.", false);
            }
            catch (Exception exception)
            {
                _store = null;
                _directoryWritable = false;
                _pathTextBox.Text = Path.Combine(directoryPath, MarkerFileStore.MarkerFileName);
                _addButton.Enabled = false;
                _removeButton.Enabled = false;
                _renumberButton.Enabled = false;
                _undoButton.Enabled = false;
                _grid.Rows.Clear();
                _emptyLabel.Visible = true;
                _countLabel.Text = "File needs attention";
                SetStatus("The marker file could not be loaded. It was not changed.", true);

                MessageBox.Show(
                    "Wade's marker file could not be read, so the app left it untouched.\r\n\r\n" + exception.Message +
                    "\r\n\r\nUse Open folder to inspect the file or restore its .backup copy.",
                    "Marker file needs attention",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                if (!selectedByUser && !Directory.Exists(directoryPath))
                {
                    HandleBrowse(this, EventArgs.Empty);
                }
            }
        }

        private void UpdateAccessState()
        {
            _directoryWritable = _store != null && MarkerFileStore.CanWriteToDirectory(_store.DirectoryPath);
            _addButton.Enabled = _directoryWritable;
            _elevateButton.Visible = !_directoryWritable;
            _accessLabel.Text = _directoryWritable
                ? "MARKER FILE  •  READY"
                : "MARKER FILE  •  WINDOWS PERMISSION NEEDED";
            _accessLabel.ForeColor = _directoryWritable ? SeaGlass : Danger;
            UpdateListActionState();

            if (!_directoryWritable)
            {
                SetStatus("Windows is protecting this game folder. Restart as administrator or choose another ClassicUO Data\\Client folder.", true);
            }
        }

        private void PopulateGrid()
        {
            _grid.Rows.Clear();
            if (_store == null)
            {
                _emptyLabel.Visible = true;
                return;
            }

            string filter = (_searchBox.Text ?? String.Empty).Trim();
            IEnumerable<MapMarker> markers = _store.Markers;
            if (filter.Length > 0)
            {
                markers = markers.Where(marker =>
                    marker.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    marker.X.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    marker.Y.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    marker.Coordinates.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            foreach (MapMarker marker in markers)
            {
                int rowIndex = _grid.Rows.Add(marker.Name, marker.X, marker.Y, "X " + marker.X + "  ·  Y " + marker.Y);
                _grid.Rows[rowIndex].Tag = marker;
            }

            int count = _store.Markers.Count;
            _countLabel.Text = count + " chart" + PluralSuffix(count) + " pinned";
            _emptyLabel.Text = filter.Length > 0
                ? "No pinned charts match that search."
                : "No bottle maps pinned yet.\r\nEnter the first set of coordinates above.";
            _emptyLabel.Visible = _grid.Rows.Count == 0;
            _grid.ClearSelection();
            _grid.CurrentCell = null;
            UpdateListActionState();
        }

        private void HandleAdd(object sender, EventArgs e)
        {
            if (_store == null)
            {
                SetStatus("Choose a valid ClassicUO Data\\Client folder first.", true);
                return;
            }

            if (!MarkerFileStore.CanWriteToDirectory(_store.DirectoryPath))
            {
                PromptForElevation();
                return;
            }

            int x;
            if (!TryReadCoordinate(_xInput, "X", out x))
            {
                return;
            }

            int y;
            if (!TryReadCoordinate(_yInput, "Y", out y))
            {
                return;
            }
            if (_store.ContainsCoordinates(x, y))
            {
                SelectMarkerAtCoordinates(x, y);
                SetStatus("That bottle map is already pinned at X " + x + ", Y " + y + ".", true);
                System.Media.SystemSounds.Exclamation.Play();
                return;
            }

            try
            {
                MapMarker marker = _store.Add(x, y);
                PopulateGrid();
                SelectMarker(marker);
                _xInput.Text = "0";
                _yInput.Text = "0";
                FocusAndSelectCoordinate(_xInput);
                SetStatus("Pinned " + marker.Name + " at X " + marker.X + ", Y " + marker.Y + " in the radar marker file. Reload markers in ClassicUO to see it.", false);
            }
            catch (UnauthorizedAccessException)
            {
                PromptForElevation();
            }
            catch (Exception exception)
            {
                ShowSaveError(exception);
            }
        }

        private void HandleRemove(object sender, EventArgs e)
        {
            if (_store == null || _grid.SelectedRows.Count == 0)
            {
                SetStatus("Select a pinned chart before marking it completed.", true);
                return;
            }

            if (!MarkerFileStore.CanWriteToDirectory(_store.DirectoryPath))
            {
                UpdateAccessState();
                PromptForElevation();
                return;
            }

            List<MapMarker> selected = new List<MapMarker>();
            foreach (DataGridViewRow row in _grid.SelectedRows)
            {
                MapMarker marker = row.Tag as MapMarker;
                if (marker != null)
                {
                    selected.Add(marker);
                }
            }

            if (selected.Count == 0)
            {
                return;
            }

            string detail = selected.Count == 1
                ? "X " + selected[0].X + ", Y " + selected[0].Y
                : selected.Count + " selected charts";
            DialogResult result = MessageBox.Show(
                "Mark " + detail + " as completed?\r\n\r\nThis removes the marker from ClassicUO. You can use Undo if needed.",
                "Complete bottle map",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes)
            {
                return;
            }

            try
            {
                int removedCount = _store.Remove(selected);
                PopulateGrid();
                SetStatus("Removed " + removedCount + " completed chart" + PluralSuffix(removedCount) + " from the radar marker file. Reload markers in ClassicUO to update the map.", false);
            }
            catch (UnauthorizedAccessException)
            {
                PromptForElevation();
            }
            catch (Exception exception)
            {
                ShowSaveError(exception);
            }
        }

        private void HandleRenumber(object sender, EventArgs e)
        {
            if (_store == null)
            {
                SetStatus("Choose a valid ClassicUO Data\\Client folder first.", true);
                return;
            }

            if (!MarkerFileStore.CanWriteToDirectory(_store.DirectoryPath))
            {
                UpdateAccessState();
                PromptForElevation();
                return;
            }

            if (!_store.NeedsSequentialRenumbering)
            {
                SetStatus("MiB labels are already in consecutive order.", false);
                UpdateListActionState();
                return;
            }

            DialogResult result = MessageBox.Show(
                "Renumber the remaining MiB labels to fill every gap?\r\n\r\nFor example, MiB 003 becomes MiB 002 when MiB 002 is gone. Coordinates and marker order will not change. You can use Undo if needed.",
                "Renumber MiB labels",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
            {
                return;
            }

            try
            {
                int changedCount = _store.RenumberSequentially();
                PopulateGrid();
                SetStatus("Renumbered " + changedCount + " MiB label" + PluralSuffix(changedCount) + " in the radar marker file. Reload markers in ClassicUO to see the updated labels.", false);
            }
            catch (UnauthorizedAccessException)
            {
                UpdateAccessState();
                PromptForElevation();
            }
            catch (Exception exception)
            {
                ShowSaveError(exception);
            }
        }

        private void HandleUndo(object sender, EventArgs e)
        {
            if (_store == null || !_store.BackupExists)
            {
                SetStatus("There is no previous change to restore.", true);
                return;
            }

            DialogResult result = MessageBox.Show(
                "Restore the marker file from its last backup?",
                "Undo last change",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes)
            {
                return;
            }

            try
            {
                _store.RestoreBackup();
                PopulateGrid();
                SetStatus("Restored the previous radar marker file. Reload markers in ClassicUO to refresh the map.", false);
            }
            catch (Exception exception)
            {
                ShowSaveError(exception);
            }
        }

        private void HandleReload(object sender, EventArgs e)
        {
            if (_store == null)
            {
                return;
            }

            try
            {
                _store.Load();
                PopulateGrid();
                UpdateAccessState();
                SetStatus("Reloaded the marker file from disk.", false);
            }
            catch (Exception exception)
            {
                SetStatus("Reload failed. The file was not changed.", true);
                MessageBox.Show(exception.Message, "Could not reload markers", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HandleBrowse(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Choose ClassicUO's Data\\Client folder";
                dialog.ShowNewFolderButton = false;
                if (_store != null && Directory.Exists(_store.DirectoryPath))
                {
                    dialog.SelectedPath = _store.DirectoryPath;
                }
                else if (Directory.Exists(_initialDirectory))
                {
                    dialog.SelectedPath = _initialDirectory;
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    LoadDirectory(dialog.SelectedPath, true);
                }
            }
        }

        private void HandleOpenFolder(object sender, EventArgs e)
        {
            string directory = _store != null ? _store.DirectoryPath : Path.GetDirectoryName(_pathTextBox.Text);
            if (String.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                SetStatus("That folder does not exist. Use Choose folder to locate ClassicUO Data\\Client.", true);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + directory + "\"") { UseShellExecute = true });
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Could not open folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HandleElevate(object sender, EventArgs e)
        {
            PromptForElevation();
        }

        private void HandleAlwaysOnTopToggle(object sender, EventArgs e)
        {
            TopMost = !TopMost;
            UpdateAlwaysOnTopState();
            SetStatus(
                TopMost
                    ? "Pinned on top. This window will stay above Outlands until you turn it off."
                    : "Keep on top is off. This window can move behind other applications.",
                false);
        }

        private void UpdateAlwaysOnTopState()
        {
            _alwaysOnTopButton.Text = TopMost ? "Pinned on top" : "Keep on top";
            _alwaysOnTopButton.BackColor = TopMost ? TreasureGold : DeepWater;
            _alwaysOnTopButton.ForeColor = TopMost ? Color.White : DeepWaterMuted;
            _alwaysOnTopButton.FlatAppearance.BorderColor = TopMost ? TreasureGold : DeepWaterMuted;
            _alwaysOnTopButton.AccessibleDescription = TopMost
                ? "This window is pinned above Outlands and other applications"
                : "Keep this window above Outlands and other applications";
            _alwaysOnTopButton.Invalidate();
        }

        private void PromptForElevation()
        {
            DialogResult result = MessageBox.Show(
                "Windows is protecting the ClassicUO game folder.\r\n\r\nRestart Wade's MiB Pinner with administrator access so it can update the marker file?",
                "Permission needed",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);
            if (result != DialogResult.Yes)
            {
                SetStatus("No changes were made. You can also choose a different ClassicUO Data\\Client folder.", true);
                return;
            }

            string directory = _store != null ? _store.DirectoryPath : _initialDirectory;
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = Application.ExecutablePath;
                startInfo.Arguments = "--folder \"" + directory + "\"";
                startInfo.UseShellExecute = true;
                startInfo.Verb = "runas";
                Process.Start(startInfo);
                Close();
            }
            catch (Win32Exception exception)
            {
                if (exception.NativeErrorCode != 1223)
                {
                    MessageBox.Show(exception.Message, "Could not restart", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void HandleGridSelectionChanged(object sender, EventArgs e)
        {
            UpdateListActionState();
        }

        private void UpdateListActionState()
        {
            bool hasStore = _store != null;
            _removeButton.Enabled = hasStore && _directoryWritable && _grid.SelectedRows.Count > 0;
            _renumberButton.Enabled = hasStore && _directoryWritable && _store.NeedsSequentialRenumbering;
            _undoButton.Enabled = hasStore && _directoryWritable && _store.BackupExists;
        }

        private void HandleFormKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && _grid.Focused && _grid.SelectedRows.Count > 0)
            {
                HandleRemove(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F5)
            {
                HandleReload(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        private void HandleCoordinateEnter(object sender, EventArgs e)
        {
            FocusAndSelectCoordinate(sender as TextBox);
        }

        private void HandleCoordinateMouseUp(object sender, MouseEventArgs e)
        {
            TextBox input = sender as TextBox;
            if (input != null)
            {
                input.SelectAll();
            }
        }

        private void HandleCoordinateKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
            {
                return;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
            _addButton.PerformClick();
        }

        private void HandleCoordinateKeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Char.IsControl(e.KeyChar) && !Char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private bool TryReadCoordinate(TextBox input, string coordinateName, out int value)
        {
            string text = input == null ? String.Empty : (input.Text ?? String.Empty).Trim();
            if (Int32.TryParse(text, out value) && value >= 0)
            {
                return true;
            }

            SetStatus(coordinateName + " must be a whole number from 0 to " + Int32.MaxValue + ".", true);
            System.Media.SystemSounds.Exclamation.Play();
            FocusAndSelectCoordinate(input);
            return false;
        }

        private static void FocusAndSelectCoordinate(TextBox input)
        {
            if (input == null)
            {
                return;
            }

            input.Focus();
            input.SelectAll();
        }

        private void SelectMarker(MapMarker marker)
        {
            _grid.ClearSelection();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (Object.ReferenceEquals(row.Tag, marker))
                {
                    row.Selected = true;
                    _grid.FirstDisplayedScrollingRowIndex = row.Index;
                    UpdateListActionState();
                    return;
                }
            }
        }

        private void SelectMarkerAtCoordinates(int x, int y)
        {
            _searchBox.Text = String.Empty;
            foreach (DataGridViewRow row in _grid.Rows)
            {
                MapMarker marker = row.Tag as MapMarker;
                if (marker != null && marker.X == x && marker.Y == y)
                {
                    _grid.ClearSelection();
                    row.Selected = true;
                    _grid.FirstDisplayedScrollingRowIndex = row.Index;
                    _grid.Focus();
                    UpdateListActionState();
                    return;
                }
            }
        }

        private void ShowSaveError(Exception exception)
        {
            SetStatus("Nothing was changed. Reload the file and try again.", true);
            MessageBox.Show(
                "The marker file could not be safely updated, so no change was made.\r\n\r\n" + exception.Message,
                "Could not save marker",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void SetStatus(string text, bool isError)
        {
            _statusLabel.Text = text;
            _statusLabel.ForeColor = isError ? Danger : MutedInk;
        }

        private static string PluralSuffix(int count)
        {
            return count == 1 ? String.Empty : "s";
        }
    }
}
