using System;
using System.Drawing;
using System.Windows.Forms;

namespace DeliveryDriverDemo
{
    /// <summary>
    /// Level-selection dialog. SelectedLevelIndex is zero-based; the game loads
    /// Levels/level{SelectedLevelIndex + 1}.txt through LevelLoader.
    /// </summary>
    internal sealed class LevelSelectionForm : Form
    {
        private ListBox _levelsList;
        private Label _descriptionLabel;

        private readonly string[] _levelNames =
        {
            "Level 1 — First Delivery",
            "Level 2 — City Traffic",
            "Level 3 — Downtown Rush"
        };

        private readonly string[] _descriptions =
        {
            "Learn the controls and plan a connected route from the depot to the customer.",
            "Choose between road options and watch the timing of the crossing traffic.",
            "A busier route-planning challenge with tighter traffic timing."
        };

        private bool _ready;

        public int SelectedLevelIndex { get; private set; }
        public string SelectedLevelName { get; private set; }

        public LevelSelectionForm()
        {
            SelectedLevelIndex = -1;
            SelectedLevelName = String.Empty;
            Text = "Select a Delivery";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(650, 420);
            BackColor = Color.FromArgb(239, 243, 246);
            Font = new Font("Segoe UI", 10f, FontStyle.Regular);

            BuildLayout();
            AudioManager.AttachClickSounds(this);
            _ready = true;
        }

        private void BuildLayout()
        {
            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(22),
                BackColor = BackColor
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
            Controls.Add(root);

            Label heading = new Label
            {
                Text = "CHOOSE YOUR DELIVERY",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 39, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            root.Controls.Add(heading, 0, 0);

            TableLayoutPanel content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 10, 0, 10)
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52f));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48f));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _levelsList = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                IntegralHeight = false,
                ItemHeight = 30,
                BorderStyle = BorderStyle.FixedSingle
            };
            for (int i = 0; i < _levelNames.Length; i++)
                _levelsList.Items.Add(_levelNames[i]);
            _levelsList.SelectedIndexChanged += LevelSelectionChanged;
            _levelsList.DoubleClick += StartClicked;
            _levelsList.SelectedIndex = 0;

            Panel details = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16),
                Margin = new Padding(12, 0, 0, 0)
            };
            Label selectedTitle = new Label
            {
                Text = "MISSION BRIEF",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(34, 143, 99)
            };
            _descriptionLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = _descriptions[0],
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Color.FromArgb(66, 80, 91),
                TextAlign = ContentAlignment.TopLeft
            };
            details.Controls.Add(_descriptionLabel);
            details.Controls.Add(selectedTitle);
            content.Controls.Add(_levelsList, 0, 0);
            content.Controls.Add(details, 1, 0);
            root.Controls.Add(content, 0, 1);

            FlowLayoutPanel buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            Button startButton = MakeButton("START LEVEL", Color.FromArgb(34, 143, 99), Color.White);
            startButton.Click += StartClicked;
            Button cancelButton = MakeButton("CANCEL", Color.FromArgb(218, 226, 231), Color.FromArgb(28, 39, 50));
            cancelButton.DialogResult = DialogResult.Cancel;
            buttons.Controls.Add(startButton);
            buttons.Controls.Add(cancelButton);
            root.Controls.Add(buttons, 0, 2);

            Label note = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Ranking: lowest total path used is #1; equal path is decided by the shorter planning time.",
                ForeColor = Color.FromArgb(100, 109, 117),
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                TextAlign = ContentAlignment.MiddleLeft
            };
            root.Controls.Add(note, 0, 3);

            AcceptButton = startButton;
            CancelButton = cancelButton;
        }

        private Button MakeButton(string text, Color back, Color fore)
        {
            Button button = new Button
            {
                Text = text,
                Width = 135,
                Height = 38,
                BackColor = back,
                ForeColor = fore,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Margin = new Padding(8, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private void LevelSelectionChanged(object sender, EventArgs e)
        {
            if (_ready) AudioManager.PlayClick();
            if (_descriptionLabel != null && _levelsList.SelectedIndex >= 0)
                _descriptionLabel.Text = _descriptions[_levelsList.SelectedIndex];
        }

        private void StartClicked(object sender, EventArgs e)
        {
            if (_levelsList.SelectedIndex < 0)
            {
                MessageBox.Show(this, "Select a level first.", "No level selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SelectedLevelIndex = _levelsList.SelectedIndex;
            SelectedLevelName = _levelNames[SelectedLevelIndex];
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}