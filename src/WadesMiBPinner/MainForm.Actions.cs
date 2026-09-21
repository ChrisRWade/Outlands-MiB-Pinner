using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WadesMiBPinner
{
    public sealed partial class MainForm
    {
        private void HandleShown(object sender, EventArgs e)
        {
            LoadDirectory(_initialDirectory, false);
            _xInput.Focus();
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
                    ? "Loaded " + _store.Markers.Count + " pinned chart" + PluralSuffix(_store.Markers.Count) + "."
                    : "Ready to create " + MarkerFileStore.MarkerFileName + " when you add the first marker.", false);
            }
            catch (Exception exception)
            {
                _store = null;
                _pathTextBox.Text = Path.Combine(directoryPath, MarkerFileStore.MarkerFileName);
                _addButton.Enabled = false;
                _removeButton.Enabled = false;
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
            bool writable = _store != null && MarkerFileStore.CanWriteToDirectory(_store.DirectoryPath);
            _addButton.Enabled = writable;
            _elevateButton.Visible = !writable;
            _elevationColumn.Width = writable ? 0F : 166F;
            _accessLabel.Text = writable
                ? "MARKER FILE  •  READY"
                : "MARKER FILE  •  WINDOWS PERMISSION NEEDED";
            _accessLabel.ForeColor = writable ? SeaGlass : Danger;
            _undoButton.Enabled = writable && _store != null && _store.BackupExists;

            if (!writable)
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
            _removeButton.Enabled = false;
            _undoButton.Enabled = MarkerFileStore.CanWriteToDirectory(_store.DirectoryPath) && _store.BackupExists;
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

            int x = Decimal.ToInt32(_xInput.Value);
            int y = Decimal.ToInt32(_yInput.Value);
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
                _xInput.Value = 0;
                _yInput.Value = 0;
                _xInput.Focus();
                SetStatus("Pinned " + marker.Name + " at X " + marker.X + ", Y " + marker.Y + ". Reload markers in ClassicUO to see it.", false);
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
                _store.Remove(selected);
                PopulateGrid();
                SetStatus("Completed " + selected.Count + " chart" + PluralSuffix(selected.Count) + ". Reload markers in ClassicUO to clear " + (selected.Count == 1 ? "it" : "them") + ".", false);
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
                SetStatus("Restored the previous marker file. Reload markers in ClassicUO to refresh the map.", false);
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
            _removeButton.Enabled = _store != null && _grid.SelectedRows.Count > 0 && MarkerFileStore.CanWriteToDirectory(_store.DirectoryPath);
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

        private void SelectMarker(MapMarker marker)
        {
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (Object.ReferenceEquals(row.Tag, marker))
                {
                    row.Selected = true;
                    _grid.FirstDisplayedScrollingRowIndex = row.Index;
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
