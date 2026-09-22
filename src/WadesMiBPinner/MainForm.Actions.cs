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
                _doneButton.Enabled = false;
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
                    marker.Coordinates.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    GetMarkerState(marker).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    marker.Icon.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            foreach (MapMarker marker in markers)
            {
                int rowIndex = _grid.Rows.Add(
                    marker.Name,
                    marker.X,
                    marker.Y,
                    GetMarkerState(marker),
                    "X " + marker.X + "  ·  Y " + marker.Y);
                DataGridViewRow row = _grid.Rows[rowIndex];
                row.Tag = marker;
                if (marker.IsCompleted)
                {
                    row.DefaultCellStyle.ForeColor = MutedInk;
                    row.DefaultCellStyle.BackColor = Color.FromArgb(241, 244, 236);
                }
            }

            int count = _store.Markers.Count;
            int doneCount = _store.Markers.Count(marker => marker.IsCompleted);
            _countLabel.Text = doneCount > 0
                ? count + " chart" + PluralSuffix(count) + "  ·  " + doneCount + " done"
                : count + " chart" + PluralSuffix(count) + " pinned";
            UpdateEmptyLabel(filter);
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
                SetStatus("Select a pinned chart before removing it.", true);
                return;
            }

            if (!MarkerFileStore.CanWriteToDirectory(_store.DirectoryPath))
            {
                UpdateAccessState();
                PromptForElevation();
                return;
            }

            List<MapMarker> selected = GetSelectedMarkers();

            if (selected.Count == 0)
            {
                return;
            }

            try
            {
                int removedCount = _store.Remove(selected);
                PopulateGrid();
                SetStatus("Removed " + removedCount + " chart" + PluralSuffix(removedCount) + " from the radar marker file. Reload markers in ClassicUO to update the map. Undo is available.", false);
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

        private void HandleDoneToggle(object sender, EventArgs e)
        {
            if (_store == null || _grid.SelectedRows.Count == 0)
            {
                SetStatus("Select one or more charts to change their map icon.", true);
                return;
            }

            if (!MarkerFileStore.CanWriteToDirectory(_store.DirectoryPath))
            {
                UpdateAccessState();
                PromptForElevation();
                return;
            }

            List<MapMarker> selected = GetSelectedMarkers();
            List<MapMarker> supported = selected.Where(marker =>
                String.Equals(marker.Icon, MarkerFileStore.DefaultIcon, StringComparison.OrdinalIgnoreCase) ||
                String.Equals(marker.Icon, MarkerFileStore.CompletedIcon, StringComparison.OrdinalIgnoreCase)).ToList();
            if (supported.Count == 0)
            {
                SetStatus("The selected marker uses a custom icon, so the app left it unchanged.", true);
                return;
            }

            bool markCompleted = supported.Any(marker => !marker.IsCompleted);
            try
            {
                int changedCount = _store.SetCompleted(supported, markCompleted);
                PopulateGrid();
                SetStatus(
                    (markCompleted ? "Marked " : "Restored ") + changedCount + " chart" + PluralSuffix(changedCount) +
                    (markCompleted ? " as done with the completed map icon." : " to the active TREASURE icon.") +
                    " Reload markers in ClassicUO to see the change.",
                    false);
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
            _alwaysOnTopButton.BackColor = TopMost ? TreasureGold : DeepWater;
            _alwaysOnTopButton.ForeColor = TopMost ? Color.White : DeepWaterMuted;
            _alwaysOnTopButton.FlatAppearance.BorderColor = TopMost ? TreasureGold : DeepWaterMuted;
            _alwaysOnTopButton.AccessibleDescription = TopMost
                ? "This window is pinned above Outlands and other applications"
                : "Keep this window above Outlands and other applications";
            UpdateButtonPresentations();
            _alwaysOnTopButton.Invalidate();
        }

        private void HandleCompactToggle(object sender, EventArgs e)
        {
            ApplyCompactMode(!_compactMode);
        }

        private void ApplyCompactMode(bool compact)
        {
            if (_compactMode == compact)
            {
                return;
            }

            SuspendLayout();
            _contentPanel.SuspendLayout();
            _mainLayout.SuspendLayout();
            try
            {
                if (compact)
                {
                    _expandedClientSize = ClientSize;
                }

                _compactMode = compact;
                _titleLabel.Visible = !compact;
                _subtitleLabel.Visible = !compact;
                _accessLabel.Visible = !compact;
                _pathTextBox.Visible = !compact;
                _entryHeading.Visible = !compact;
                _entryHelp.Visible = !compact;
                _listHeading.Visible = !compact;
                _searchLabel.Visible = !compact;
                _footerPanel.Visible = !compact;
                UpdateEmptyLabel((_searchBox.Text ?? String.Empty).Trim());

                if (compact)
                {
                    _headerPanel.Height = 82;
                    _headerPanel.Padding = new Padding(8);
                    _headerLayout.ColumnStyles[0] = new ColumnStyle(SizeType.Absolute, 0F);
                    _headerLayout.ColumnStyles[1] = new ColumnStyle(SizeType.Percent, 100F);
                    _headerStatus.MinimumSize = new Size(0, 0);
                    _headerStatus.Margin = new Padding(0);
                    _contentPanel.Padding = new Padding(8);
                    _mainLayout.RowStyles[0] = new RowStyle(SizeType.Absolute, 54F);
                    _mainLayout.RowStyles[1] = new RowStyle(SizeType.Absolute, 88F);
                    _mainLayout.RowStyles[3] = new RowStyle(SizeType.Absolute, 0F);
                    _locationPanel.Padding = new Padding(6);
                    _locationPanel.Margin = new Padding(0, 0, 0, 6);
                    _locationLayout.RowStyles[0] = new RowStyle(SizeType.Absolute, 0F);
                    _locationLayout.RowStyles[1] = new RowStyle(SizeType.Absolute, 0F);
                    _locationLayout.RowStyles[2] = new RowStyle(SizeType.Percent, 100F);
                    _entryPanel.Padding = new Padding(8, 6, 8, 6);
                    _entryPanel.Margin = new Padding(0, 0, 0, 6);
                    _entryLayout.ColumnStyles[0] = new ColumnStyle(SizeType.Absolute, 0F);
                    _entryLayout.ColumnStyles[1] = new ColumnStyle(SizeType.Percent, 50F);
                    _entryLayout.ColumnStyles[2] = new ColumnStyle(SizeType.Percent, 50F);
                    _entryLayout.ColumnStyles[3] = new ColumnStyle(SizeType.Absolute, 8F);
                    _entryLayout.ColumnStyles[4] = new ColumnStyle(SizeType.Absolute, 48F);
                    _entryLayout.RowStyles[0] = new RowStyle(SizeType.Absolute, 22F);
                    _entryLayout.RowStyles[1] = new RowStyle(SizeType.Percent, 100F);
                    _xField.Margin = new Padding(0, 0, 4, 0);
                    _yField.Margin = new Padding(4, 0, 0, 0);
                    _addButton.Margin = new Padding(0, 17, 0, 0);
                    _addButton.MinimumSize = new Size(44, 44);
                    _listLayout.RowStyles[0] = new RowStyle(SizeType.Absolute, 54F);
                    _listToolbar.Padding = new Padding(8, 7, 8, 7);
                    _listToolbar.ColumnStyles[0] = new ColumnStyle(SizeType.Absolute, 0F);
                    _listToolbar.ColumnStyles[1] = new ColumnStyle(SizeType.Absolute, 0F);
                    _listToolbar.ColumnStyles[2] = new ColumnStyle(SizeType.Percent, 100F);
                    _searchBox.MinimumSize = new Size(110, 0);
                    MinimumSize = new Size(640, 480);
                    ClientSize = new Size(680, 520);
                }
                else
                {
                    _headerPanel.Height = 112;
                    _headerPanel.Padding = new Padding(28, 14, 28, 14);
                    _headerLayout.ColumnStyles[0] = new ColumnStyle(SizeType.Percent, 100F);
                    _headerLayout.ColumnStyles[1] = new ColumnStyle(SizeType.AutoSize);
                    _headerStatus.MinimumSize = new Size(300, 0);
                    _headerStatus.Margin = new Padding(24, 0, 0, 0);
                    _contentPanel.Padding = new Padding(24, 20, 24, 18);
                    _mainLayout.RowStyles[0] = new RowStyle(SizeType.Absolute, 132F);
                    _mainLayout.RowStyles[1] = new RowStyle(SizeType.Absolute, 140F);
                    _mainLayout.RowStyles[3] = new RowStyle(SizeType.Absolute, 82F);
                    _locationPanel.Padding = new Padding(14, 10, 14, 10);
                    _locationPanel.Margin = new Padding(0, 0, 0, 12);
                    _locationLayout.RowStyles[0] = new RowStyle(SizeType.AutoSize);
                    _locationLayout.RowStyles[1] = new RowStyle(SizeType.Absolute, 34F);
                    _locationLayout.RowStyles[2] = new RowStyle(SizeType.Percent, 100F);
                    _entryPanel.Padding = new Padding(16, 12, 16, 14);
                    _entryPanel.Margin = new Padding(0, 0, 0, 12);
                    _entryLayout.ColumnStyles[0] = new ColumnStyle(SizeType.Percent, 100F);
                    _entryLayout.ColumnStyles[1] = new ColumnStyle(SizeType.AutoSize);
                    _entryLayout.ColumnStyles[2] = new ColumnStyle(SizeType.AutoSize);
                    _entryLayout.ColumnStyles[3] = new ColumnStyle(SizeType.Absolute, 12F);
                    _entryLayout.ColumnStyles[4] = new ColumnStyle(SizeType.AutoSize);
                    _entryLayout.RowStyles[0] = new RowStyle(SizeType.Absolute, 30F);
                    _entryLayout.RowStyles[1] = new RowStyle(SizeType.Percent, 100F);
                    _xField.Margin = new Padding(8, 0, 8, 0);
                    _yField.Margin = new Padding(8, 0, 8, 0);
                    _addButton.Margin = new Padding(0, 19, 0, 0);
                    _addButton.MinimumSize = new Size(160, 52);
                    _listLayout.RowStyles[0] = new RowStyle(SizeType.Absolute, 62F);
                    _listToolbar.Padding = new Padding(16, 10, 12, 8);
                    _listToolbar.ColumnStyles[0] = new ColumnStyle(SizeType.Percent, 100F);
                    _listToolbar.ColumnStyles[1] = new ColumnStyle(SizeType.AutoSize);
                    _listToolbar.ColumnStyles[2] = new ColumnStyle(SizeType.AutoSize);
                    _searchBox.MinimumSize = new Size(170, 0);
                    MinimumSize = new Size(1000, 760);
                    ClientSize = _expandedClientSize.Width >= 1000 && _expandedClientSize.Height >= 720
                        ? _expandedClientSize
                        : new Size(1120, 760);
                }

                UpdateButtonPresentations();
                PerformLayout();
                Invalidate(true);
            }
            finally
            {
                _mainLayout.ResumeLayout(true);
                _contentPanel.ResumeLayout(true);
                ResumeLayout(true);
            }

            FocusAndSelectCoordinate(_xInput);
        }

        private void UpdateButtonPresentations()
        {
            PresentButton(_alwaysOnTopButton, TopMost ? "Pinned on top" : "Keep on top", TopMost ? "↓" : "↑", TopMost ? "Stop keeping this window on top" : "Keep this window on top");
            PresentButton(_compactButton, "Compact view", "▣", _compactMode ? "Return to full view" : "Switch to compact view");
            PresentButton(_browseButton, "Choose folder", "…", "Choose marker folder");
            PresentButton(_openButton, "Open folder", "↗", "Open marker folder");
            PresentButton(_elevateButton, "Restart as administrator", "◆", "Restart as administrator");
            PresentButton(_addButton, "Pin this MiB", "+", "Pin this MiB");
            PresentButton(_reloadButton, "Reload", "↻", "Reload marker file");
            PresentButton(_undoButton, "Undo", "↶", "Undo last marker change");
            PresentButton(_renumberButton, "Renumber", "№", "Renumber MiB labels");
            PresentButton(_doneButton, SelectedMarkersAreAllCompleted() ? "Mark active" : "Mark done", SelectedMarkersAreAllCompleted() ? "●" : "✓", SelectedMarkersAreAllCompleted() ? "Restore selected MiBs to active TREASURE pins" : "Mark selected MiBs done");
            PresentButton(_removeButton, "Remove", "×", "Remove selected MiBs immediately");
        }

        private void PresentButton(Button button, string fullText, string compactText, string tooltip)
        {
            if (button == null)
            {
                return;
            }

            button.Text = _compactMode ? compactText : fullText;
            button.Font = _compactMode
                ? CompactButtonFont
                : button == _addButton ? PrimaryButtonFont : StandardButtonFont;
            button.AutoSize = !_compactMode && button != _alwaysOnTopButton && button != _compactButton;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.Padding = _compactMode ? new Padding(0) : new Padding(14, 0, 14, 0);
            if (_compactMode)
            {
                button.MinimumSize = button == _alwaysOnTopButton || button == _compactButton
                    ? new Size(0, 38)
                    : new Size(42, 40);
            }
            else
            {
                button.MinimumSize = button == _addButton
                    ? new Size(160, 52)
                    : button == _alwaysOnTopButton || button == _compactButton
                        ? new Size(0, 38)
                        : new Size(0, 40);
            }
            _toolTip.SetToolTip(button, tooltip);
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
                startInfo.Arguments = "--folder \"" + directory + "\" --wait-for-existing-instance";
                startInfo.UseShellExecute = true;
                startInfo.Verb = "runas";
                using (Process elevatedProcess = Process.Start(startInfo))
                {
                }
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
            List<MapMarker> selected = GetSelectedMarkers();
            bool hasSelection = selected.Count > 0;
            bool hasSupportedIcon = selected.Any(marker =>
                String.Equals(marker.Icon, MarkerFileStore.DefaultIcon, StringComparison.OrdinalIgnoreCase) ||
                String.Equals(marker.Icon, MarkerFileStore.CompletedIcon, StringComparison.OrdinalIgnoreCase));
            _removeButton.Enabled = hasStore && _directoryWritable && hasSelection;
            _doneButton.Enabled = hasStore && _directoryWritable && hasSupportedIcon;
            _renumberButton.Enabled = hasStore && _directoryWritable && _store.NeedsSequentialRenumbering;
            _undoButton.Enabled = hasStore && _directoryWritable && _store.BackupExists;
            UpdateButtonPresentations();
        }

        private List<MapMarker> GetSelectedMarkers()
        {
            List<MapMarker> selected = new List<MapMarker>();
            foreach (DataGridViewRow row in _grid.SelectedRows)
            {
                MapMarker marker = row.Tag as MapMarker;
                if (marker != null && !selected.Contains(marker))
                {
                    selected.Add(marker);
                }
            }

            return selected;
        }

        private bool SelectedMarkersAreAllCompleted()
        {
            List<MapMarker> selected = GetSelectedMarkers().Where(marker =>
                String.Equals(marker.Icon, MarkerFileStore.DefaultIcon, StringComparison.OrdinalIgnoreCase) ||
                String.Equals(marker.Icon, MarkerFileStore.CompletedIcon, StringComparison.OrdinalIgnoreCase)).ToList();
            return selected.Count > 0 && selected.All(marker => marker.IsCompleted);
        }

        private static string GetMarkerState(MapMarker marker)
        {
            if (marker.IsCompleted)
            {
                return "DONE";
            }

            return String.Equals(marker.Icon, MarkerFileStore.DefaultIcon, StringComparison.OrdinalIgnoreCase)
                ? "ACTIVE"
                : "CUSTOM";
        }

        private void UpdateEmptyLabel(string filter)
        {
            _emptyLabel.Text = _compactMode
                ? (filter.Length > 0 ? "No matches" : "No charts")
                : filter.Length > 0
                    ? "No pinned charts match that search."
                    : "No bottle maps pinned yet.\r\nEnter the first set of coordinates above.";
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
