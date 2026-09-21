using System;
using System.Drawing;
using System.Windows.Forms;

namespace WadesMiBPinner
{
    public sealed partial class MainForm : Form
    {
        private static readonly Color DeepWater = Color.FromArgb(24, 45, 53);
        private static readonly Color DeepWaterMuted = Color.FromArgb(170, 194, 197);
        private static readonly Color Parchment = Color.FromArgb(244, 238, 220);
        private static readonly Color Paper = Color.FromArgb(255, 253, 247);
        private static readonly Color Ink = Color.FromArgb(38, 48, 50);
        private static readonly Color MutedInk = Color.FromArgb(100, 108, 107);
        private static readonly Color SeaGlass = Color.FromArgb(45, 103, 114);
        private static readonly Color TreasureGold = Color.FromArgb(184, 135, 58);
        private static readonly Color Danger = Color.FromArgb(156, 66, 57);
        private static readonly Color Hairline = Color.FromArgb(218, 208, 185);
        private static readonly Color Inset = Color.FromArgb(238, 232, 216);

        private readonly string _initialDirectory;
        private MarkerFileStore _store;

        private Label _countLabel;
        private TextBox _pathTextBox;
        private Label _accessLabel;
        private Button _elevateButton;
        private NumericUpDown _xInput;
        private NumericUpDown _yInput;
        private Button _addButton;
        private Button _removeButton;
        private Button _undoButton;
        private DataGridView _grid;
        private Label _emptyLabel;
        private TextBox _searchBox;
        private Label _statusLabel;

        public MainForm(string initialDirectory)
        {
            _initialDirectory = initialDirectory;
            BuildInterface();
            Shown += HandleShown;
        }

        private void BuildInterface()
        {
            Text = "Wade's MiB Pinner";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1000, 760);
            ClientSize = new Size(1120, 760);
            BackColor = Parchment;
            ForeColor = Ink;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            AutoScaleMode = AutoScaleMode.Dpi;
            KeyPreview = true;

            try
            {
                Icon executableIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (executableIcon != null)
                {
                    Icon = executableIcon;
                }
            }
            catch
            {
                // A missing shell icon should never prevent the app from opening.
            }

            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 112;
            header.Padding = new Padding(28, 14, 28, 14);
            header.BackColor = DeepWater;
            Controls.Add(header);

            TableLayoutPanel headerLayout = new TableLayoutPanel();
            headerLayout.Dock = DockStyle.Fill;
            headerLayout.ColumnCount = 2;
            headerLayout.RowCount = 2;
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 62F));
            headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));
            header.Controls.Add(headerLayout);

            Label title = new Label();
            title.AutoSize = true;
            title.Text = "Wade's MiB Pinner";
            title.ForeColor = Color.White;
            title.Font = new Font("Georgia", 22F, FontStyle.Bold, GraphicsUnit.Point);
            title.Dock = DockStyle.Fill;
            title.TextAlign = ContentAlignment.BottomLeft;
            title.Margin = new Padding(0);
            headerLayout.Controls.Add(title, 0, 0);

            Label subtitle = new Label();
            subtitle.AutoSize = true;
            subtitle.Text = "Adds map pins to the marker file ClassicUO's radar loads.";
            subtitle.ForeColor = DeepWaterMuted;
            subtitle.Dock = DockStyle.Fill;
            subtitle.TextAlign = ContentAlignment.TopLeft;
            subtitle.Margin = new Padding(1, 0, 0, 0);
            headerLayout.Controls.Add(subtitle, 0, 1);

            _countLabel = new Label();
            _countLabel.AutoSize = true;
            _countLabel.Dock = DockStyle.Fill;
            _countLabel.MinimumSize = new Size(0, 40);
            _countLabel.Padding = new Padding(18, 0, 18, 0);
            _countLabel.Margin = new Padding(24, 10, 0, 10);
            _countLabel.TextAlign = ContentAlignment.MiddleCenter;
            _countLabel.BackColor = SeaGlass;
            _countLabel.ForeColor = Color.White;
            _countLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            _countLabel.Text = "0 charts pinned";
            headerLayout.Controls.Add(_countLabel, 1, 0);
            headerLayout.SetRowSpan(_countLabel, 2);

            Panel content = new Panel();
            content.Dock = DockStyle.Fill;
            content.Padding = new Padding(24, 20, 24, 18);
            content.BackColor = Parchment;
            Controls.Add(content);
            content.BringToFront();

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 4;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 84F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82F));
            content.Controls.Add(layout);

            layout.Controls.Add(BuildLocationPanel(), 0, 0);
            layout.Controls.Add(BuildEntryPanel(), 0, 1);
            layout.Controls.Add(BuildListPanel(), 0, 2);
            layout.Controls.Add(BuildFooterPanel(), 0, 3);

            AcceptButton = _addButton;
            KeyDown += HandleFormKeyDown;
        }

        private Control BuildLocationPanel()
        {
            Panel panel = CreateSurfacePanel(new Padding(14, 10, 14, 10));
            panel.Margin = new Padding(0, 0, 0, 12);

            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.ColumnCount = 4;
            table.RowCount = 2;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.Controls.Add(table);

            _accessLabel = new Label();
            _accessLabel.AutoSize = true;
            _accessLabel.Text = "MARKER FILE";
            _accessLabel.Font = new Font("Segoe UI", 8F, FontStyle.Bold, GraphicsUnit.Point);
            _accessLabel.ForeColor = MutedInk;
            table.Controls.Add(_accessLabel, 0, 0);
            table.SetColumnSpan(_accessLabel, 4);

            _pathTextBox = new TextBox();
            _pathTextBox.Dock = DockStyle.Fill;
            _pathTextBox.ReadOnly = true;
            _pathTextBox.BackColor = Inset;
            _pathTextBox.BorderStyle = BorderStyle.FixedSingle;
            _pathTextBox.ForeColor = Ink;
            _pathTextBox.AccessibleName = "Current marker file";
            _pathTextBox.Margin = new Padding(0, 2, 10, 0);
            table.Controls.Add(_pathTextBox, 0, 1);

            Button browseButton = CreateButton("Choose folder", Paper, Ink);
            browseButton.Margin = new Padding(0, 0, 8, 0);
            browseButton.Click += HandleBrowse;
            table.Controls.Add(browseButton, 1, 1);

            Button openButton = CreateButton("Open folder", Paper, Ink);
            openButton.Margin = new Padding(0, 0, 8, 0);
            openButton.Click += HandleOpenFolder;
            table.Controls.Add(openButton, 2, 1);

            _elevateButton = CreateButton("Restart as administrator", TreasureGold, Color.White);
            _elevateButton.Margin = new Padding(0);
            _elevateButton.Click += HandleElevate;
            table.Controls.Add(_elevateButton, 3, 1);

            return panel;
        }

        private Control BuildEntryPanel()
        {
            Panel panel = CreateSurfacePanel(new Padding(16, 12, 16, 14));
            panel.Margin = new Padding(0, 0, 0, 12);

            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.ColumnCount = 5;
            table.RowCount = 2;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.Controls.Add(table);

            Label heading = new Label();
            heading.AutoSize = true;
            heading.Text = "Pin a new bottle map";
            heading.Font = new Font("Georgia", 13F, FontStyle.Bold, GraphicsUnit.Point);
            heading.ForeColor = DeepWater;
            heading.Margin = new Padding(0, 2, 0, 0);
            table.Controls.Add(heading, 0, 0);

            Label help = new Label();
            help.AutoSize = false;
            help.Dock = DockStyle.Fill;
            help.Text = "Enter the X and Y from the bottle. Each entry becomes a TREASURE pin in Wade's marker file.";
            help.ForeColor = MutedInk;
            help.Margin = new Padding(0, 10, 0, 0);
            table.Controls.Add(help, 0, 1);

            Control xField = BuildCoordinateField("X COORDINATE", out _xInput);
            table.Controls.Add(xField, 1, 0);
            table.SetRowSpan(xField, 2);
            Control yField = BuildCoordinateField("Y COORDINATE", out _yInput);
            table.Controls.Add(yField, 2, 0);
            table.SetRowSpan(yField, 2);

            _addButton = CreateButton("Pin this MiB", SeaGlass, Color.White);
            _addButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            _addButton.Margin = new Padding(0, 19, 0, 0);
            _addButton.MinimumSize = new Size(160, 52);
            _addButton.AccessibleDescription = "Add a treasure marker at the entered coordinates";
            _addButton.Click += HandleAdd;
            table.Controls.Add(_addButton, 4, 0);
            table.SetRowSpan(_addButton, 2);

            return panel;
        }

        private Control BuildCoordinateField(string labelText, out NumericUpDown input)
        {
            TableLayoutPanel field = new TableLayoutPanel();
            field.Dock = DockStyle.Fill;
            field.AutoSize = true;
            field.MinimumSize = new Size(150, 0);
            field.RowCount = 2;
            field.ColumnCount = 1;
            field.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            field.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            field.Margin = new Padding(8, 0, 8, 0);

            Label label = new Label();
            label.AutoSize = true;
            label.Text = labelText;
            label.Font = new Font("Segoe UI", 8F, FontStyle.Bold, GraphicsUnit.Point);
            label.ForeColor = MutedInk;
            field.Controls.Add(label, 0, 0);

            NumericUpDown createdInput = new NumericUpDown();
            createdInput.Dock = DockStyle.Fill;
            createdInput.AutoSize = true;
            createdInput.MinimumSize = new Size(150, 0);
            createdInput.Minimum = 0;
            createdInput.Maximum = Int32.MaxValue;
            createdInput.DecimalPlaces = 0;
            createdInput.Font = new Font("Consolas", 15F, FontStyle.Bold, GraphicsUnit.Point);
            createdInput.TextAlign = HorizontalAlignment.Center;
            createdInput.BackColor = Inset;
            createdInput.ForeColor = Ink;
            createdInput.BorderStyle = BorderStyle.FixedSingle;
            createdInput.AccessibleName = labelText == "X COORDINATE" ? "X coordinate" : "Y coordinate";
            field.Controls.Add(createdInput, 0, 1);
            label.Click += delegate { createdInput.Focus(); };
            input = createdInput;

            return field;
        }

        private Control BuildListPanel()
        {
            Panel panel = CreateSurfacePanel(new Padding(0));
            TableLayoutPanel listLayout = new TableLayoutPanel();
            listLayout.Dock = DockStyle.Fill;
            listLayout.ColumnCount = 1;
            listLayout.RowCount = 2;
            listLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            listLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.Controls.Add(listLayout);

            TableLayoutPanel toolbar = new TableLayoutPanel();
            toolbar.Dock = DockStyle.Fill;
            toolbar.Padding = new Padding(16, 10, 12, 8);
            toolbar.BackColor = Paper;
            toolbar.ColumnCount = 6;
            toolbar.RowCount = 1;
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            listLayout.Controls.Add(toolbar, 0, 0);

            Label label = new Label();
            label.AutoSize = true;
            label.Text = "Pinned charts";
            label.ForeColor = DeepWater;
            label.Font = new Font("Georgia", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Margin = new Padding(4, 0, 12, 0);
            toolbar.Controls.Add(label, 0, 0);

            _removeButton = CreateButton("Mark completed", Danger, Color.White);
            _removeButton.Margin = new Padding(8, 0, 0, 0);
            _removeButton.Enabled = false;
            _removeButton.Click += HandleRemove;
            toolbar.Controls.Add(_removeButton, 5, 0);

            _undoButton = CreateButton("Undo", Paper, Ink);
            _undoButton.Margin = new Padding(8, 0, 0, 0);
            _undoButton.Enabled = false;
            _undoButton.Click += HandleUndo;
            toolbar.Controls.Add(_undoButton, 4, 0);

            Button reloadButton = CreateButton("Reload", Paper, Ink);
            reloadButton.Margin = new Padding(8, 0, 0, 0);
            reloadButton.Click += HandleReload;
            toolbar.Controls.Add(reloadButton, 3, 0);

            _searchBox = new TextBox();
            _searchBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _searchBox.MinimumSize = new Size(170, 0);
            _searchBox.Margin = new Padding(8, 5, 0, 5);
            _searchBox.BackColor = Inset;
            _searchBox.BorderStyle = BorderStyle.FixedSingle;
            _searchBox.AccessibleName = "Search pinned charts";
            _searchBox.TextChanged += delegate { PopulateGrid(); };
            toolbar.Controls.Add(_searchBox, 2, 0);

            Label searchLabel = new Label();
            searchLabel.AutoSize = true;
            searchLabel.Dock = DockStyle.Fill;
            searchLabel.Text = "Search";
            searchLabel.ForeColor = MutedInk;
            searchLabel.TextAlign = ContentAlignment.MiddleRight;
            searchLabel.Margin = new Padding(0, 0, 0, 0);
            toolbar.Controls.Add(searchLabel, 1, 0);

            _grid = CreateGrid();
            _grid.SelectionChanged += HandleGridSelectionChanged;

            Panel gridHost = new Panel();
            gridHost.Dock = DockStyle.Fill;
            gridHost.BackColor = Paper;
            gridHost.Controls.Add(_grid);
            listLayout.Controls.Add(gridHost, 0, 1);

            _emptyLabel = new Label();
            _emptyLabel.Dock = DockStyle.Fill;
            _emptyLabel.BackColor = Paper;
            _emptyLabel.ForeColor = MutedInk;
            _emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
            _emptyLabel.Font = new Font("Georgia", 12F, FontStyle.Italic, GraphicsUnit.Point);
            _emptyLabel.Text = "No bottle maps pinned yet.\r\nEnter the first set of coordinates above.";
            _emptyLabel.Visible = false;
            gridHost.Controls.Add(_emptyLabel);
            _emptyLabel.BringToFront();

            return panel;
        }

        private DataGridView CreateGrid()
        {
            DataGridView grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.BackgroundColor = Paper;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Hairline;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Inset;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = MutedInk;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            grid.RowHeadersVisible = false;
            grid.RowTemplate.MinimumHeight = 36;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
            grid.DefaultCellStyle.BackColor = Paper;
            grid.DefaultCellStyle.ForeColor = Ink;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(214, 230, 231);
            grid.DefaultCellStyle.SelectionForeColor = DeepWater;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = true;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AutoGenerateColumns = false;
            grid.AccessibleName = "Pinned bottle maps";

            grid.Columns.Add(CreateTextColumn("MarkerName", "CHART", 140, false));
            DataGridViewTextBoxColumn xColumn = CreateTextColumn("X", "X", 80, false);
            xColumn.DefaultCellStyle.Font = new Font("Consolas", 10F, FontStyle.Bold, GraphicsUnit.Point);
            grid.Columns.Add(xColumn);
            DataGridViewTextBoxColumn yColumn = CreateTextColumn("Y", "Y", 80, false);
            yColumn.DefaultCellStyle.Font = new Font("Consolas", 10F, FontStyle.Bold, GraphicsUnit.Point);
            grid.Columns.Add(yColumn);
            DataGridViewTextBoxColumn coordinates = CreateTextColumn("Coordinates", "MAP COORDINATES", 200, true);
            coordinates.SortMode = DataGridViewColumnSortMode.NotSortable;
            grid.Columns.Add(coordinates);
            return grid;
        }

        private static DataGridViewTextBoxColumn CreateTextColumn(string name, string header, int width, bool fill)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.Name = name;
            column.HeaderText = header;
            column.SortMode = DataGridViewColumnSortMode.Automatic;
            if (fill)
            {
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
            else
            {
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                column.MinimumWidth = width;
            }
            return column;
        }

        private Control BuildFooterPanel()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(2, 8, 2, 0);

            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.ColumnCount = 1;
            table.RowCount = 2;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            panel.Controls.Add(table);

            _statusLabel = new Label();
            _statusLabel.Dock = DockStyle.Fill;
            _statusLabel.ForeColor = MutedInk;
            _statusLabel.Text = "Ready.";
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            table.Controls.Add(_statusLabel, 0, 0);

            Label reloadHelp = new Label();
            reloadHelp.Dock = DockStyle.Fill;
            reloadHelp.ForeColor = SeaGlass;
            reloadHelp.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
            reloadHelp.Text = "To see changes in game: World Map → right-click → Map Marker Options → Reload markers";
            reloadHelp.TextAlign = ContentAlignment.MiddleLeft;
            table.Controls.Add(reloadHelp, 0, 1);
            return panel;
        }

        private static Panel CreateSurfacePanel(Padding padding)
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = padding;
            panel.BackColor = Paper;
            panel.Paint += delegate(object sender, PaintEventArgs e)
            {
                Control control = (Control)sender;
                using (Pen pen = new Pen(Hairline))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, control.ClientSize.Width - 1, control.ClientSize.Height - 1);
                }
            };
            return panel;
        }

        private static Button CreateButton(string text, Color backColor, Color foreColor)
        {
            Button button = new Button();
            button.Text = text;
            button.Dock = DockStyle.Fill;
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.MinimumSize = new Size(0, 40);
            button.Padding = new Padding(14, 0, 14, 0);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = backColor == Paper ? Hairline : backColor;
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = backColor;
            button.ForeColor = foreColor;
            button.Cursor = Cursors.Hand;
            button.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            button.UseVisualStyleBackColor = false;
            return button;
        }
    }
}
