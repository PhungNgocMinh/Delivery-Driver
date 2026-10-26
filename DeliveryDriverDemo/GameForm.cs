using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace DeliveryDriverDemo
{
    internal sealed class GameForm : Form
    {
        private readonly GameEngine _engine;
        private readonly GamePanel _gamePanel;
        private readonly Timer _timer;
        private readonly Stopwatch _clock;

        private Label _stateLabel;
        private Label _timeLabel;
        private Label _pathLabel;
        private Label _resultLabel;
        private Button _goButton;
        private Button _undoButton;
        private Button _clearButton;
        private Button _retryButton;
        private long _lastTickMs;

        public GameForm()
        {
            _engine = new GameEngine();
            _gamePanel = new GamePanel(_engine);
            _timer = new Timer { Interval = 16 };
            _clock = new Stopwatch();

            Text = "Delivery Driver — WinForms MVP";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1050, 700);
            BackColor = Color.FromArgb(235, 239, 242);
            Font = new Font("Segoe UI", 10f, FontStyle.Regular);

            BuildInterface();
            WireEvents();
            RefreshInterface();

            _clock.Start();
            _lastTickMs = _clock.ElapsedMilliseconds;
            _timer.Start();
        }

        private void BuildInterface()
        {
            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = Color.FromArgb(235, 239, 242)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            Controls.Add(root);

            Panel hud = BuildHud();
            TableLayoutPanel content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = Color.FromArgb(235, 239, 242)
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 258f));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            _gamePanel.Dock = DockStyle.Fill;
            Panel sidebar = BuildSidebar();
            content.Controls.Add(_gamePanel, 0, 0);
            content.Controls.Add(sidebar, 1, 0);
            root.Controls.Add(hud, 0, 0);
            root.Controls.Add(content, 0, 1);
        }

        private Panel BuildHud()
        {
            Panel hud = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(28, 39, 50),
                Padding = new Padding(22, 0, 10, 0),
                Margin = Padding.Empty
            };
            FlowLayoutPanel items = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            Label title = CreateHudLabel("DELIVERY DRIVER", 13f, true);
            title.Margin = new Padding(0, 17, 42, 0);
            _stateLabel = CreateHudLabel("STATE  PLANNING", 10.5f, true);
            _stateLabel.Margin = new Padding(0, 20, 30, 0);
            _timeLabel = CreateHudLabel("TIME  45 s", 10.5f, true);
            _timeLabel.Margin = new Padding(0, 20, 30, 0);
            _pathLabel = CreateHudLabel("PATH  0 / 32", 10.5f, true);
            _pathLabel.Margin = new Padding(0, 20, 30, 0);

            items.Controls.Add(title);
            items.Controls.Add(_stateLabel);
            items.Controls.Add(_timeLabel);
            items.Controls.Add(_pathLabel);
            hud.Controls.Add(items);
            return hud;
        }

        private Label CreateHudLabel(string text, float size, bool bold)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = Color.FromArgb(239, 245, 248),
                Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private Panel BuildSidebar()
        {
            Panel sidebar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(247, 249, 250),
                Padding = new Padding(15, 16, 15, 12),
                Margin = Padding.Empty
            };
            FlowLayoutPanel stack = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            Label heading = new Label
            {
                Text = "PLAN YOUR DELIVERY",
                AutoSize = true,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(34, 49, 62),
                Margin = new Padding(0, 0, 0, 7)
            };
            Label instructions = new Label
            {
                Text = "1. Press on S and drag along the roads.\n\n" +
                       "2. Stay on connected road tiles. Drag back one tile to undo.\n\n" +
                       "3. Reach F, then press GO. Traffic starts moving and your van follows the route.\n\n" +
                       "Fast route = risky. The lower detour is longer but can be safer.",
                AutoSize = true,
                MaximumSize = new Size(215, 0),
                Font = new Font("Segoe UI", 9.2f, FontStyle.Regular),
                ForeColor = Color.FromArgb(76, 88, 99),
                Margin = new Padding(0, 0, 0, 12)
            };

            _goButton = CreateButton("GO  ▶", Color.FromArgb(34, 143, 99), Color.White);
            _undoButton = CreateButton("Undo last tile", Color.FromArgb(225, 231, 235), Color.FromArgb(43, 55, 65));
            _clearButton = CreateButton("Clear route", Color.FromArgb(225, 231, 235), Color.FromArgb(43, 55, 65));
            _retryButton = CreateButton("Retry / Reset", Color.FromArgb(49, 102, 155), Color.White);

            _resultLabel = new Label
            {
                Text = String.Empty,
                AutoSize = true,
                MaximumSize = new Size(215, 0),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(32, 102, 78),
                Margin = new Padding(0, 7, 0, 12),
                Visible = false
            };

            Label legend = new Label
            {
                Text = "TILE GUIDE\nS  Depot / start\nF  Customer / finish\nBrown  Mud (slow)\nBlue-grey  Highway (fast)\nRed / purple  Traffic",
                AutoSize = true,
                MaximumSize = new Size(215, 0),
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(90, 102, 113),
                Margin = new Padding(0, 5, 0, 0)
            };

            stack.Controls.Add(heading);
            stack.Controls.Add(instructions);
            stack.Controls.Add(_goButton);
            stack.Controls.Add(_undoButton);
            stack.Controls.Add(_clearButton);
            stack.Controls.Add(_retryButton);
            stack.Controls.Add(_resultLabel);
            stack.Controls.Add(legend);
            sidebar.Controls.Add(stack);
            return sidebar;
        }

        private Button CreateButton(string text, Color backColor, Color foreColor)
        {
            return new Button
            {
                Text = text,
                Width = 215,
                Height = 42,
                BackColor = backColor,
                ForeColor = foreColor,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 8),
                UseVisualStyleBackColor = false
            };
        }

        private void WireEvents()
        {
            _goButton.Click += delegate
            {
                _engine.Go();
                _lastTickMs = _clock.ElapsedMilliseconds;
                _gamePanel.Invalidate();
                RefreshInterface();
            };

            _undoButton.Click += delegate
            {
                _engine.UndoPath();
                _gamePanel.CancelDrawing();
                _gamePanel.Invalidate();
                RefreshInterface();
            };

            _clearButton.Click += delegate
            {
                _engine.ClearPath();
                _gamePanel.CancelDrawing();
                _gamePanel.Invalidate();
                RefreshInterface();
            };

            _retryButton.Click += delegate
            {
                _engine.Retry();
                _gamePanel.CancelDrawing();
                _gamePanel.Invalidate();
                RefreshInterface();
            };

            _timer.Tick += TimerTick;
            FormClosing += delegate
            {
                _timer.Stop();
                _clock.Stop();
            };
        }

        private void TimerTick(object sender, EventArgs e)
        {
            long now = _clock.ElapsedMilliseconds;
            int dt = (int)Math.Max(0, Math.Min(250, now - _lastTickMs));
            _lastTickMs = now;

            GameState previousState = _engine.State;
            _engine.Update(dt);
            if (previousState != _engine.State)
            {
                _gamePanel.CancelDrawing();
            }

            _gamePanel.Invalidate();
            RefreshInterface();
        }

        private void RefreshInterface()
        {
            _stateLabel.Text = "STATE  " + _engine.State.ToString().ToUpperInvariant();
            _timeLabel.Text = "TIME  " + _engine.RemainingSeconds + " s";
            _pathLabel.Text = "PATH  " + _engine.PathLength + " / " + _engine.InkLimit;

            _goButton.Enabled = _engine.CanGo;
            _undoButton.Enabled = _engine.State == GameState.Planning && _engine.PathLength > 1;
            _clearButton.Enabled = _engine.State == GameState.Planning && _engine.PathLength > 0;
            _retryButton.Text = (_engine.State == GameState.Planning) ? "Restart route" : "Retry / Reset";

            bool hasResult = _engine.State == GameState.Won || _engine.State == GameState.Lost;
            _resultLabel.Visible = hasResult;
            _resultLabel.Text = hasResult ? _engine.ResultMessage : String.Empty;
            _resultLabel.ForeColor = _engine.State == GameState.Won
                ? Color.FromArgb(32, 122, 77)
                : Color.FromArgb(171, 56, 48);
        }
    }
}
