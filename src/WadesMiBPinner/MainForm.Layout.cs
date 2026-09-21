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
        private ColumnStyle _elevationColumn;

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
            MinimumSize = new Size(900, 650);
            Size = new Size(1040, 720);
            BackColor = Parchment;
            ForeColor = Ink;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            AutoScaleMode = AutoScaleMode.Dpi;
            KeyPreview = true;

            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 104;
            header.BackColor = DeepWater;
            Controls.Add(header);

            Label title = new Label();
            title.AutoSize = true;
            title.Text = "Wade's MiB Pinner";
            title.ForeColor = Color.White;
            title.Font = new Font("Georgia", 22F, FontStyle.Bold, GraphicsUnit.Point);
            title.Location = new Point(28, 20);
            header.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.AutoSize = true;
            subtitle.Text = "Adds map pins to the marker file ClassicUO's radar loads.";
            subtitle.ForeColor = DeepWaterMuted;
            subtitle.Location = new Point(31, 62);
            header.Controls.Add(subtitle);

            _countLabel = new Label();
            _countLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _countLabel.AutoSize = false;
            _countLabel.Size = new Size(150, 38);
            _countLabel.Location = new Point(ClientSize.Width - 180, 31);
            _countLabel.TextAlign = ContentAlignment.MiddleCenter;
            _countLabel.BackColor = SeaGlass;
            _countLabel.ForeColor = Color.White;
            _countLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            _countLabel.Text = "0 charts pinned";
            header.Controls.Add(_countLabel);

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
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 132F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
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
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 102F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 102F));
            _elevationColumn = new ColumnStyle(SizeType.Absolute, 166F);
            table.ColumnStyles.Add(_elevationColumn);
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
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
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 18F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
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
            help.AutoSize = true;
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
            _addButton.Height = 52;
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
            listLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            listLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.Controls.Add(listLayout);

            Panel toolbar = new Panel();
            toolbar.Dock = DockStyle.Fill;
            toolbar.Padding = new Padding(16, 10, 12, 8);
            toolbar.BackColor = Paper;
            listLayout.Controls.Add(toolbar, 0, 0);

            Label label = new Label();
            label.AutoSize = true;
            label.Text = "Pinned charts";
            label.ForeColor = DeepWater;
            label.Font = new Font("Georgia", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label.Location = new Point(16, 17);
            toolbar.Controls.Add(label);

            _removeButton = CreateButton("Mark completed", Danger, Color.White);
            _removeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _removeButton.Size = new Size(132, 38);
            _removeButton.Location = new Point(toolbar.Width - 144, 10);
            _removeButton.Enabled = false;
            _removeButton.Click += HandleRemove;
            toolbar.Controls.Add(_removeButton);

            _undoButton = CreateButton("Undo", Paper, Ink);
            _undoButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _undoButton.Size = new Size(70, 38);
            _undoButton.Location = new Point(toolbar.Width - 222, 10);
            _undoButton.Enabled = false;
            _undoButton.Click += HandleUndo;
            toolbar.Controls.Add(_undoButton);

            Button reloadButton = CreateButton("Reload", Paper, Ink);
            reloadButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            reloadButton.Size = new Size(74, 38);
            reloadButton.Location = new Point(toolbar.Width - 304, 10);
            reloadButton.Click += HandleReload;
            toolbar.Controls.Add(reloadButton);

            _searchBox = new TextBox();
            _searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _searchBox.Size = new Size(154, 28);
            _searchBox.Location = new Point(toolbar.Width - 468, 15);
            _searchBox.BackColor = Inset;
            _searchBox.BorderStyle = BorderStyle.FixedSingle;
            _searchBox.AccessibleName = "Search pinned charts";
            _searchBox.TextChanged += delegate { PopulateGrid(); };
            toolbar.Controls.Add(_searchBox);

            Label searchLabel = new Label();
            searchLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            searchLabel.AutoSize = true;
            searchLabel.Text = "Search";
            searchLabel.ForeColor = MutedInk;
            searchLabel.Location = new Point(toolbar.Width - 518, 19);
            toolbar.Controls.Add(searchLabel);

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

            toolbar.Resize += delegate
            {
                _removeButton.Left = toolbar.ClientSize.Width - 144;
                _undoButton.Left = toolbar.ClientSize.Width - 222;
                reloadButton.Left = toolbar.ClientSize.Width - 304;
                _searchBox.Left = toolbar.ClientSize.Width - 468;
                searchLabel.Left = toolbar.ClientSize.Width - 518;
            };

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
            grid.ColumnHeadersHeight = 36;
            grid.RowHeadersVisible = false;
            grid.RowTemplate.Height = 36;
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

            grid.Columns.Add(CreateTextColumn("MarkerName", "CHART", 150, false));
            DataGridViewTextBoxColumn xColumn = CreateTextColumn("X", "X", 120, false);
            xColumn.DefaultCellStyle.Font = new Font("Consolas", 10F, FontStyle.Bold, GraphicsUnit.Point);
            grid.Columns.Add(xColumn);
            DataGridViewTextBoxColumn yColumn = CreateTextColumn("Y", "Y", 120, false);
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
            column.Width = width;
            column.SortMode = DataGridViewColumnSortMode.Automatic;
            if (fill)
            {
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
            return column;
        }

        private Control BuildFooterPanel()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(2, 13, 2, 0);

            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.ColumnCount = 2;
            table.RowCount = 1;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            panel.Controls.Add(table);

            _statusLabel = new Label();
            _statusLabel.Dock = DockStyle.Fill;
            _statusLabel.ForeColor = MutedInk;
            _statusLabel.Text = "Ready.";
            _statusLabel.TextAlign = ContentAlignment.TopLeft;
            table.Controls.Add(_statusLabel, 0, 0);

            Label reloadHelp = new Label();
            reloadHelp.Dock = DockStyle.Fill;
            reloadHelp.ForeColor = MutedInk;
            reloadHelp.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            reloadHelp.Text = "To see changes in game: World Map → right-click → Map Marker Options → Reload markers";
            reloadHelp.TextAlign = ContentAlignment.TopRight;
            table.Controls.Add(reloadHelp, 1, 0);
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
            button.Height = 40;
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
