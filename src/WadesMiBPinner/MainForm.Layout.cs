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
        private static readonly Font StandardButtonFont = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        private static readonly Font PrimaryButtonFont = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
        private static readonly Font CompactButtonFont = new Font("Segoe UI Symbol", 14F, FontStyle.Bold, GraphicsUnit.Point);

        private readonly string _initialDirectory;
        private MarkerFileStore _store;

        private Label _countLabel;
        private Button _alwaysOnTopButton;
        private Button _compactButton;
        private TextBox _pathTextBox;
        private Label _accessLabel;
        private Button _elevateButton;
        private Button _browseButton;
        private Button _openButton;
        private TextBox _xInput;
        private TextBox _yInput;
        private Button _addButton;
        private Button _doneButton;
        private Button _removeButton;
        private Button _renumberButton;
        private Button _undoButton;
        private Button _reloadButton;
        private DataGridView _grid;
        private Label _emptyLabel;
        private TextBox _searchBox;
        private Label _statusLabel;
        private Label _titleLabel;
        private Label _subtitleLabel;
        private Label _entryHeading;
        private Label _entryHelp;
        private Label _listHeading;
        private Label _searchLabel;
        private Panel _headerPanel;
        private Panel _contentPanel;
        private Panel _locationPanel;
        private Panel _entryPanel;
        private Panel _listPanel;
        private Panel _footerPanel;
        private TableLayoutPanel _headerLayout;
        private TableLayoutPanel _headerStatus;
        private TableLayoutPanel _mainLayout;
        private TableLayoutPanel _locationLayout;
        private TableLayoutPanel _locationActions;
        private TableLayoutPanel _entryLayout;
        private TableLayoutPanel _listLayout;
        private TableLayoutPanel _listToolbar;
        private Control _xField;
        private Control _yField;
        private readonly ToolTip _toolTip = new ToolTip();
        private bool _compactMode;
        private Size _expandedClientSize;

        public MainForm(string initialDirectory)
        {
            _initialDirectory = initialDirectory;
            _toolTip.InitialDelay = 100;
            _toolTip.ReshowDelay = 50;
            _toolTip.AutoPopDelay = 5000;
            _toolTip.ShowAlways = true;
            BuildInterface();
            Shown += HandleShown;
        }

        private void BuildInterface()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            UpdateStyles();

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

            _headerPanel = new BufferedPanel();
            _headerPanel.Dock = DockStyle.Top;
            _headerPanel.Height = 112;
            _headerPanel.Padding = new Padding(28, 14, 28, 14);
            _headerPanel.BackColor = DeepWater;
            Controls.Add(_headerPanel);

            _headerLayout = new BufferedTableLayoutPanel();
            _headerLayout.Dock = DockStyle.Fill;
            _headerLayout.BackColor = DeepWater;
            _headerLayout.ColumnCount = 2;
            _headerLayout.RowCount = 2;
            _headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 62F));
            _headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));
            _headerPanel.Controls.Add(_headerLayout);

            _titleLabel = new Label();
            _titleLabel.AutoSize = true;
            _titleLabel.Text = "Wade's MiB Pinner";
            _titleLabel.ForeColor = Color.White;
            _titleLabel.Font = new Font("Georgia", 22F, FontStyle.Bold, GraphicsUnit.Point);
            _titleLabel.Dock = DockStyle.Fill;
            _titleLabel.TextAlign = ContentAlignment.BottomLeft;
            _titleLabel.Margin = new Padding(0);
            _headerLayout.Controls.Add(_titleLabel, 0, 0);

            _subtitleLabel = new Label();
            _subtitleLabel.AutoSize = true;
            _subtitleLabel.Text = "Adds map pins to the marker file ClassicUO's radar loads.";
            _subtitleLabel.ForeColor = DeepWaterMuted;
            _subtitleLabel.Dock = DockStyle.Fill;
            _subtitleLabel.TextAlign = ContentAlignment.TopLeft;
            _subtitleLabel.Margin = new Padding(1, 0, 0, 0);
            _headerLayout.Controls.Add(_subtitleLabel, 0, 1);

            _headerStatus = new BufferedTableLayoutPanel();
            _headerStatus.Dock = DockStyle.Fill;
            _headerStatus.AutoSize = true;
            _headerStatus.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _headerStatus.MinimumSize = new Size(300, 0);
            _headerStatus.BackColor = DeepWater;
            _headerStatus.ColumnCount = 2;
            _headerStatus.RowCount = 2;
            _headerStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            _headerStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            _headerStatus.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            _headerStatus.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            _headerStatus.Margin = new Padding(24, 0, 0, 0);
            _headerLayout.Controls.Add(_headerStatus, 1, 0);
            _headerLayout.SetRowSpan(_headerStatus, 2);

            _countLabel = new Label();
            _countLabel.AutoSize = false;
            _countLabel.Dock = DockStyle.Fill;
            _countLabel.MinimumSize = new Size(190, 38);
            _countLabel.Padding = new Padding(18, 0, 18, 0);
            _countLabel.Margin = new Padding(0, 0, 0, 2);
            _countLabel.TextAlign = ContentAlignment.MiddleCenter;
            _countLabel.BackColor = SeaGlass;
            _countLabel.ForeColor = Color.White;
            _countLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            _countLabel.Text = "0 charts pinned";
            _headerStatus.Controls.Add(_countLabel, 0, 0);
            _headerStatus.SetColumnSpan(_countLabel, 2);

            _alwaysOnTopButton = CreateButton("Keep on top", DeepWater, DeepWaterMuted);
            _alwaysOnTopButton.AutoSize = false;
            _alwaysOnTopButton.MinimumSize = new Size(0, 38);
            _alwaysOnTopButton.Margin = new Padding(0, 2, 4, 0);
            _alwaysOnTopButton.Padding = new Padding(12, 0, 12, 0);
            _alwaysOnTopButton.FlatAppearance.BorderColor = DeepWaterMuted;
            _alwaysOnTopButton.AccessibleName = "Keep window on top";
            _alwaysOnTopButton.AccessibleDescription = "Keep this window above Outlands and other applications";
            _alwaysOnTopButton.Click += HandleAlwaysOnTopToggle;
            _headerStatus.Controls.Add(_alwaysOnTopButton, 0, 1);

            _compactButton = CreateButton("Compact view", DeepWater, DeepWaterMuted);
            _compactButton.AutoSize = false;
            _compactButton.MinimumSize = new Size(0, 38);
            _compactButton.Margin = new Padding(4, 2, 0, 0);
            _compactButton.Padding = new Padding(12, 0, 12, 0);
            _compactButton.FlatAppearance.BorderColor = DeepWaterMuted;
            _compactButton.AccessibleName = "Toggle compact view";
            _compactButton.AccessibleDescription = "Switch between the full explanation and the compact coordinate console";
            _compactButton.Click += HandleCompactToggle;
            _headerStatus.Controls.Add(_compactButton, 1, 1);

            _contentPanel = new BufferedPanel();
            _contentPanel.Dock = DockStyle.Fill;
            _contentPanel.Padding = new Padding(24, 20, 24, 18);
            _contentPanel.BackColor = Parchment;
            Controls.Add(_contentPanel);
            _contentPanel.BringToFront();

            _mainLayout = new BufferedTableLayoutPanel();
            _mainLayout.Dock = DockStyle.Fill;
            _mainLayout.BackColor = Parchment;
            _mainLayout.ColumnCount = 1;
            _mainLayout.RowCount = 4;
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 132F));
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F));
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82F));
            _contentPanel.Controls.Add(_mainLayout);

            _mainLayout.Controls.Add(BuildLocationPanel(), 0, 0);
            _mainLayout.Controls.Add(BuildEntryPanel(), 0, 1);
            _mainLayout.Controls.Add(BuildListPanel(), 0, 2);
            _mainLayout.Controls.Add(BuildFooterPanel(), 0, 3);

            AcceptButton = _addButton;
            KeyDown += HandleFormKeyDown;
            UpdateButtonPresentations();
        }

        private Control BuildLocationPanel()
        {
            _locationPanel = CreateSurfacePanel(new Padding(14, 10, 14, 10));
            _locationPanel.Margin = new Padding(0, 0, 0, 12);

            _locationLayout = new BufferedTableLayoutPanel();
            _locationLayout.Dock = DockStyle.Fill;
            _locationLayout.BackColor = Paper;
            _locationLayout.ColumnCount = 1;
            _locationLayout.RowCount = 3;
            _locationLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _locationLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _locationLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            _locationLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _locationPanel.Controls.Add(_locationLayout);

            _accessLabel = new Label();
            _accessLabel.AutoSize = true;
            _accessLabel.Text = "MARKER FILE";
            _accessLabel.Font = new Font("Segoe UI", 8F, FontStyle.Bold, GraphicsUnit.Point);
            _accessLabel.ForeColor = MutedInk;
            _locationLayout.Controls.Add(_accessLabel, 0, 0);

            _pathTextBox = new TextBox();
            _pathTextBox.Dock = DockStyle.Fill;
            _pathTextBox.ReadOnly = true;
            _pathTextBox.BackColor = Inset;
            _pathTextBox.BorderStyle = BorderStyle.FixedSingle;
            _pathTextBox.ForeColor = Ink;
            _pathTextBox.AccessibleName = "Current marker file";
            _pathTextBox.Margin = new Padding(0, 2, 0, 6);
            _locationLayout.Controls.Add(_pathTextBox, 0, 1);

            _locationActions = new BufferedTableLayoutPanel();
            _locationActions.Dock = DockStyle.Fill;
            _locationActions.BackColor = Paper;
            _locationActions.ColumnCount = 4;
            _locationActions.RowCount = 1;
            _locationActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _locationActions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _locationActions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _locationActions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _locationActions.Margin = new Padding(0);
            _locationLayout.Controls.Add(_locationActions, 0, 2);

            _browseButton = CreateButton("Choose folder", Paper, Ink);
            _browseButton.Margin = new Padding(0, 0, 8, 0);
            _browseButton.AccessibleName = "Choose marker folder";
            _browseButton.Click += HandleBrowse;
            _locationActions.Controls.Add(_browseButton, 1, 0);

            _openButton = CreateButton("Open folder", Paper, Ink);
            _openButton.Margin = new Padding(0, 0, 8, 0);
            _openButton.AccessibleName = "Open marker folder";
            _openButton.Click += HandleOpenFolder;
            _locationActions.Controls.Add(_openButton, 2, 0);

            _elevateButton = CreateButton("Restart as administrator", TreasureGold, Color.White);
            _elevateButton.Margin = new Padding(0);
            _elevateButton.Click += HandleElevate;
            _elevateButton.AccessibleName = "Restart as administrator";
            _locationActions.Controls.Add(_elevateButton, 3, 0);

            return _locationPanel;
        }

        private Control BuildEntryPanel()
        {
            _entryPanel = CreateSurfacePanel(new Padding(16, 12, 16, 14));
            _entryPanel.Margin = new Padding(0, 0, 0, 12);

            _entryLayout = new BufferedTableLayoutPanel();
            _entryLayout.Dock = DockStyle.Fill;
            _entryLayout.BackColor = Paper;
            _entryLayout.ColumnCount = 5;
            _entryLayout.RowCount = 2;
            _entryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _entryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _entryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _entryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12F));
            _entryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _entryLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            _entryLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _entryPanel.Controls.Add(_entryLayout);

            _entryHeading = new Label();
            _entryHeading.AutoSize = true;
            _entryHeading.Text = "Pin a new bottle map";
            _entryHeading.Font = new Font("Georgia", 13F, FontStyle.Bold, GraphicsUnit.Point);
            _entryHeading.ForeColor = DeepWater;
            _entryHeading.Margin = new Padding(0, 2, 0, 0);
            _entryLayout.Controls.Add(_entryHeading, 0, 0);

            _entryHelp = new Label();
            _entryHelp.AutoSize = false;
            _entryHelp.Dock = DockStyle.Fill;
            _entryHelp.Text = "Enter the X and Y from the bottle. Each entry becomes a TREASURE pin in Wade's marker file.";
            _entryHelp.ForeColor = MutedInk;
            _entryHelp.Margin = new Padding(0, 10, 0, 0);
            _entryLayout.Controls.Add(_entryHelp, 0, 1);

            _xField = BuildCoordinateField("X COORDINATE", out _xInput);
            _entryLayout.Controls.Add(_xField, 1, 0);
            _entryLayout.SetRowSpan(_xField, 2);
            _yField = BuildCoordinateField("Y COORDINATE", out _yInput);
            _entryLayout.Controls.Add(_yField, 2, 0);
            _entryLayout.SetRowSpan(_yField, 2);

            _addButton = CreateButton("Pin this MiB", SeaGlass, Color.White);
            _addButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            _addButton.Margin = new Padding(0, 19, 0, 0);
            _addButton.MinimumSize = new Size(160, 52);
            _addButton.AccessibleName = "Pin this MiB";
            _addButton.AccessibleDescription = "Add a treasure marker at the entered coordinates";
            _addButton.Click += HandleAdd;
            _entryLayout.Controls.Add(_addButton, 4, 0);
            _entryLayout.SetRowSpan(_addButton, 2);

            return _entryPanel;
        }

        private Control BuildCoordinateField(string labelText, out TextBox input)
        {
            TableLayoutPanel field = new BufferedTableLayoutPanel();
            field.Dock = DockStyle.Fill;
            field.BackColor = Paper;
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

            TextBox createdInput = new TextBox();
            createdInput.Dock = DockStyle.Fill;
            createdInput.MinimumSize = new Size(150, 0);
            createdInput.MaxLength = 10;
            createdInput.Text = "0";
            createdInput.Font = new Font("Consolas", 15F, FontStyle.Bold, GraphicsUnit.Point);
            createdInput.TextAlign = HorizontalAlignment.Center;
            createdInput.BackColor = Inset;
            createdInput.ForeColor = Ink;
            createdInput.BorderStyle = BorderStyle.FixedSingle;
            createdInput.AccessibleName = labelText == "X COORDINATE" ? "X coordinate" : "Y coordinate";
            createdInput.Enter += HandleCoordinateEnter;
            createdInput.MouseUp += HandleCoordinateMouseUp;
            createdInput.KeyDown += HandleCoordinateKeyDown;
            createdInput.KeyPress += HandleCoordinateKeyPress;
            field.Controls.Add(createdInput, 0, 1);
            label.Click += delegate { FocusAndSelectCoordinate(createdInput); };
            input = createdInput;

            return field;
        }

        private Control BuildListPanel()
        {
            _listPanel = CreateSurfacePanel(new Padding(0));
            _listLayout = new BufferedTableLayoutPanel();
            _listLayout.Dock = DockStyle.Fill;
            _listLayout.BackColor = Paper;
            _listLayout.ColumnCount = 1;
            _listLayout.RowCount = 2;
            _listLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            _listLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _listPanel.Controls.Add(_listLayout);

            _listToolbar = new BufferedTableLayoutPanel();
            _listToolbar.Dock = DockStyle.Fill;
            _listToolbar.Padding = new Padding(16, 10, 12, 8);
            _listToolbar.BackColor = Paper;
            _listToolbar.ColumnCount = 8;
            _listToolbar.RowCount = 1;
            _listToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _listToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _listToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _listToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _listToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _listToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _listToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _listToolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _listLayout.Controls.Add(_listToolbar, 0, 0);

            _listHeading = new Label();
            _listHeading.AutoSize = true;
            _listHeading.Text = "Pinned charts";
            _listHeading.ForeColor = DeepWater;
            _listHeading.Font = new Font("Georgia", 12F, FontStyle.Bold, GraphicsUnit.Point);
            _listHeading.Dock = DockStyle.Fill;
            _listHeading.TextAlign = ContentAlignment.MiddleLeft;
            _listHeading.Margin = new Padding(4, 0, 12, 0);
            _listToolbar.Controls.Add(_listHeading, 0, 0);

            _removeButton = CreateButton("Remove", Danger, Color.White);
            _removeButton.Margin = new Padding(8, 0, 0, 0);
            _removeButton.Enabled = false;
            _removeButton.AccessibleName = "Remove selected MiBs";
            _removeButton.AccessibleDescription = "Immediately remove the selected coordinates from the marker file";
            _removeButton.Click += HandleRemove;
            _listToolbar.Controls.Add(_removeButton, 7, 0);

            _doneButton = CreateButton("Mark done", SeaGlass, Color.White);
            _doneButton.Margin = new Padding(8, 0, 0, 0);
            _doneButton.Enabled = false;
            _doneButton.AccessibleName = "Toggle selected MiBs done";
            _doneButton.AccessibleDescription = "Change selected treasure pins to completed map icons without removing them";
            _doneButton.Click += HandleDoneToggle;
            _listToolbar.Controls.Add(_doneButton, 6, 0);

            _renumberButton = CreateButton("Renumber", Paper, Ink);
            _renumberButton.Margin = new Padding(8, 0, 0, 0);
            _renumberButton.Enabled = false;
            _renumberButton.AccessibleDescription = "Fill gaps in MiB chart labels without changing coordinates";
            _renumberButton.Click += HandleRenumber;
            _renumberButton.AccessibleName = "Renumber MiB labels";
            _listToolbar.Controls.Add(_renumberButton, 5, 0);

            _undoButton = CreateButton("Undo", Paper, Ink);
            _undoButton.Margin = new Padding(8, 0, 0, 0);
            _undoButton.Enabled = false;
            _undoButton.Click += HandleUndo;
            _undoButton.AccessibleName = "Undo last marker change";
            _listToolbar.Controls.Add(_undoButton, 4, 0);

            _reloadButton = CreateButton("Reload", Paper, Ink);
            _reloadButton.Margin = new Padding(8, 0, 0, 0);
            _reloadButton.AccessibleName = "Reload marker file";
            _reloadButton.Click += HandleReload;
            _listToolbar.Controls.Add(_reloadButton, 3, 0);

            _searchBox = new TextBox();
            _searchBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _searchBox.MinimumSize = new Size(170, 0);
            _searchBox.Margin = new Padding(8, 5, 0, 5);
            _searchBox.BackColor = Inset;
            _searchBox.BorderStyle = BorderStyle.FixedSingle;
            _searchBox.AccessibleName = "Search pinned charts";
            _searchBox.TextChanged += delegate { PopulateGrid(); };
            _listToolbar.Controls.Add(_searchBox, 2, 0);

            _searchLabel = new Label();
            _searchLabel.AutoSize = true;
            _searchLabel.Dock = DockStyle.Fill;
            _searchLabel.Text = "Search";
            _searchLabel.ForeColor = MutedInk;
            _searchLabel.TextAlign = ContentAlignment.MiddleRight;
            _searchLabel.Margin = new Padding(0, 0, 0, 0);
            _listToolbar.Controls.Add(_searchLabel, 1, 0);

            _grid = CreateGrid();
            _grid.SelectionChanged += HandleGridSelectionChanged;

            Panel gridHost = new BufferedPanel();
            gridHost.Dock = DockStyle.Fill;
            gridHost.BackColor = Paper;
            gridHost.Controls.Add(_grid);
            _listLayout.Controls.Add(gridHost, 0, 1);

            _emptyLabel = new Label();
            _emptyLabel.Dock = DockStyle.Fill;
            _emptyLabel.BackColor = Paper;
            _emptyLabel.ForeColor = MutedInk;
            _emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
            _emptyLabel.Font = new Font("Georgia", 12F, FontStyle.Italic, GraphicsUnit.Point);
            _emptyLabel.Text = "No bottle maps pinned yet.\r\nEnter the first set of coordinates above.";
            _emptyLabel.AccessibleName = "Empty chart list";
            _emptyLabel.Visible = false;
            gridHost.Controls.Add(_emptyLabel);
            _emptyLabel.BringToFront();

            return _listPanel;
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
            DataGridViewTextBoxColumn stateColumn = CreateTextColumn("State", "STATUS", 90, false);
            stateColumn.DefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
            grid.Columns.Add(stateColumn);
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
            _footerPanel = new BufferedPanel();
            _footerPanel.Dock = DockStyle.Fill;
            _footerPanel.Padding = new Padding(2, 8, 2, 0);
            _footerPanel.BackColor = Parchment;

            TableLayoutPanel table = new BufferedTableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.BackColor = Parchment;
            table.ColumnCount = 1;
            table.RowCount = 2;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            _footerPanel.Controls.Add(table);

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
            return _footerPanel;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _toolTip.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (IsHandleCreated)
            {
                Invalidate(true);
            }
        }

        protected override void OnResizeEnd(EventArgs e)
        {
            base.OnResizeEnd(e);
            if (IsHandleCreated)
            {
                Refresh();
            }
        }

        private static Panel CreateSurfacePanel(Padding padding)
        {
            Panel panel = new SurfacePanel(Hairline);
            panel.Dock = DockStyle.Fill;
            panel.Padding = padding;
            panel.BackColor = Paper;
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

    internal class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            UpdateStyles();
        }
    }

    internal sealed class SurfacePanel : BufferedPanel
    {
        private readonly Color _borderColor;

        public SurfacePanel(Color borderColor)
        {
            _borderColor = borderColor;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
            {
                return;
            }

            using (Pen pen = new Pen(_borderColor))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
            }
        }
    }

    internal sealed class BufferedTableLayoutPanel : TableLayoutPanel
    {
        public BufferedTableLayoutPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            UpdateStyles();
        }
    }
}
