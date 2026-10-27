using DeliveryDriverDemo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace DeliveryDriverDemo
{
    internal sealed class GameForm : Form
    {
        private readonly GameEngine _engine;
        private readonly GamePanel _gamePanel;
        private const int MaxLevelNumber = 3; //khoi tao so luong level toi da vi game nay la game choi theo man 
        private readonly string _sessionId = Guid.NewGuid().ToString("N");
        private ScoreRecord _sessionRecord;
        private bool _playerNameAsked;
        private bool _leaderboardShown;
        private readonly Timer _timer;
        private readonly Stopwatch _clock;
        private Button _pauseButton; //them nut pause
        private Button _resumeButton; //them nut resume
        private Button _nextLevelButton; //them nut de qua man tiep theo
        private Label _stateLabel;
        private Label _timeLabel;
        private Label _pathLabel;
        private Label _scoreLabel;
        private Label _resultLabel;
        private Button _goButton;
        private Button _undoButton;
        private Button _clearButton;
        private Button _retryButton;
        private long _lastTickMs;
        public GameForm() : this(1)
        {
        }

        public GameForm(int levelNumber)
        {
            _engine = new GameEngine(levelNumber);
            _sessionRecord = new ScoreRecord
            {
                SessionId = _sessionId,
                Name = "Player"
            };
            _gamePanel = new GamePanel(_engine);
            _timer = new Timer { Interval = 16 };
            _clock = new Stopwatch();

            Text = "Delivery Driver";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1050, 700);
            BackColor = Color.FromArgb(235, 239, 242);
            Font = new Font("Segoe UI", 10f, FontStyle.Regular);

            BuildInterface();
            WireEvents();
            RefreshInterface();
            AudioManager.AttachClickSounds(this);
            AudioManager.StartMusic();

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
            _scoreLabel = CreateHudLabel("TOTAL PATH  -", 10.5f, true);
            _scoreLabel.Margin = new Padding(0, 20, 30, 0);

            items.Controls.Add(title);
            items.Controls.Add(_stateLabel);
            items.Controls.Add(_timeLabel);
            items.Controls.Add(_pathLabel);
            items.Controls.Add(_scoreLabel);
            // Mute button and volume bar in the upper-right corner (M also toggles mute).
            AudioControlPanel audio = new AudioControlPanel(true)
            {
                Dock = DockStyle.Right,
                Width = 290
            };

            hud.Controls.Add(items);    // Fill first, then the right-docked audio panel
            hud.Controls.Add(audio);
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
            _pauseButton = CreateButton("Pause  ||",Color.FromArgb(225, 160, 55),Color.White);
            _resumeButton = CreateButton("Resume  ▶", Color.FromArgb(34, 143, 99), Color.White);
            _undoButton = CreateButton("Undo last tile", Color.FromArgb(225, 231, 235), Color.FromArgb(43, 55, 65));
            _clearButton = CreateButton("Clear route", Color.FromArgb(225, 231, 235), Color.FromArgb(43, 55, 65));
            _retryButton = CreateButton("Retry / Reset", Color.FromArgb(49, 102, 155), Color.White);
            _nextLevelButton = CreateButton("Next Level  ▶", Color.FromArgb(49, 102, 155), Color.White);

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
                Text = "TILE GUIDE\nS  Depot / start\nF  Customer / finish\nBrown  Mud (slow)\nBlue-grey  Highway (fast)\nRed / purple  Traffic\n\nKeys: P = pause, M = mute",
                AutoSize = true,
                MaximumSize = new Size(215, 0),
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(90, 102, 113),
                Margin = new Padding(0, 5, 0, 0)
            };

            stack.Controls.Add(heading);
            stack.Controls.Add(instructions);
            stack.Controls.Add(_goButton);
            stack.Controls.Add(_pauseButton);
            stack.Controls.Add(_resumeButton);
            stack.Controls.Add(_undoButton);
            stack.Controls.Add(_clearButton);
            stack.Controls.Add(_retryButton);
            stack.Controls.Add(_nextLevelButton);
            stack.Controls.Add(_resultLabel);
            stack.Controls.Add(legend);
            sidebar.Controls.Add(stack);
            return sidebar;
        }

        private string PromptPlayerName() // tao hop thoai nhap ten neu bo qua thi tne se la Player
        {
            using (Form prompt = new Form())
            {
                prompt.Text = "Luu thanh tich";
                prompt.StartPosition = FormStartPosition.CenterParent;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.ClientSize = new Size(390, 145);
                prompt.MaximizeBox = false;
                prompt.MinimizeBox = false;
                prompt.ShowInTaskbar = false;

                Label label = new Label
                {
                    Text = "Nhap ten nguoi choi:",
                    Left = 16,
                    Top = 12,
                    Width = 350,
                    Height = 24
                };

                TextBox nameBox = new TextBox
                {
                    Left = 16,
                    Top = 40,
                    Width = 355,
                    MaxLength = 40
                };

                Button saveButton = new Button
                {
                    Text = "Lưu",
                    Left = 190,
                    Top = 83,
                    Width = 85,
                    DialogResult = DialogResult.OK
                };

                Button cancelButton = new Button
                {
                    Text = "Bỏ qua",
                    Left = 285,
                    Top = 83,
                    Width = 85,
                    DialogResult = DialogResult.Cancel
                };

                prompt.Controls.Add(label);
                prompt.Controls.Add(nameBox);
                prompt.Controls.Add(saveButton);
                prompt.Controls.Add(cancelButton);

                prompt.AcceptButton = saveButton;
                prompt.CancelButton = cancelButton;

                AudioManager.AttachClickSounds(prompt);

                if (prompt.ShowDialog(this) != DialogResult.OK)
                    return "Player";

                string name = nameBox.Text.Trim();

                return string.IsNullOrWhiteSpace(name)
                    ? "Player"
                    : name;
            }
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
            _pauseButton.Click += delegate { PauseGame(); };
            _resumeButton.Click += delegate { ResumeGame(); };

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

            // nut retry va next level chi hien khi nguoi choi thang hoac thua man do
            _retryButton.Click += delegate
            {
                if (_engine.State == GameState.Won) // kiem tra xem nguoi choi da thang man nay chua
                {
                    // xoa diem cua man hien tai neu nhan nut retry khi thang
                    _sessionRecord.ClearLevelResult(
                        _engine.CurrentLevelNumber);

                    TrySaveSession();
                }

                _leaderboardShown = false;

                _engine.Retry();

                _gamePanel.CancelDrawing();
                _lastTickMs = _clock.ElapsedMilliseconds;

                _gamePanel.Invalidate();
                RefreshInterface();
            };
            
            _nextLevelButton.Click += delegate
            {
                if (_engine.State != GameState.Won)
                    return;

                if (_engine.CurrentLevelNumber >= MaxLevelNumber)
                    return;

                int nextLevel = _engine.CurrentLevelNumber + 1;

                _engine.LoadLevel(nextLevel);

                _gamePanel.CancelDrawing();

                _lastTickMs = _clock.ElapsedMilliseconds;

                _gamePanel.Invalidate();
                RefreshInterface();
            };
            
            _timer.Tick += TimerTick;
            FormClosing += OnGameFormClosing;
            FormClosed += delegate { _timer.Dispose(); };
        }

        private void OnGameFormClosing(object sender, FormClosingEventArgs e)
        {
            // Ask before abandoning a delivery that is in progress.
            if (_engine.State == GameState.Running || _engine.State == GameState.Paused)
            {
                bool wasRunning = _engine.State == GameState.Running;
                if (wasRunning) PauseGame();

                DialogResult answer = MessageBox.Show(this,
                    "Leave this delivery? Progress in the current level will be lost.",
                    "Leave game?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (answer != DialogResult.Yes)
                {
                    e.Cancel = true;
                    if (wasRunning) ResumeGame();
                    return;
                }
            }

            _timer.Stop();
            _clock.Stop();
            AudioManager.SetEngineRunning(false);
        }

        private void PauseGame()
        {
            if (_engine.State != GameState.Planning && _engine.State != GameState.Running)
                return;

            _engine.Pause();
            _gamePanel.CancelDrawing();
            _lastTickMs = _clock.ElapsedMilliseconds;   // no time jump on resume
            _gamePanel.Invalidate();
            RefreshInterface();
        }

        private void ResumeGame()
        {
            if (_engine.State != GameState.Paused)
                return;

            _engine.Resume();
            _lastTickMs = _clock.ElapsedMilliseconds;   // no time jump on resume
            _gamePanel.Invalidate();
            RefreshInterface();
        }

        // P toggles pause even when a button has keyboard focus.
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.P)
            {
                if (_engine.State == GameState.Paused) ResumeGame();
                else PauseGame();
                return true;
            }

            // M toggles mute while playing.
            if (keyData == Keys.M)
            {
                AudioManager.Muted = !AudioManager.Muted;
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Saving can fail (locked or read-only file); never let that crash the game.
        private void TrySaveSession()
        {
            try
            {
                ScoreBoard.SaveOrUpdate(_sessionRecord);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Could not save your score:\n" + ex.Message,
                    "Leaderboard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SaveCurrentLevelResult() // luu ket qua cua man hien tai vao bang thanh tich
        {
            if (!_playerNameAsked)
            {
                _sessionRecord.Name = PromptPlayerName();
                _playerNameAsked = true;
            }

            _sessionRecord.SetLevelResult(
                _engine.CurrentLevelNumber,
                _engine.LastLevelPathUsed,
                _engine.LastLevelPlanSeconds);

            TrySaveSession();
        }

        private void ShowLeaderboardAtEnd() // hien thi bang thanh tich khi ket thuc man choi
        {
            if (_leaderboardShown)
                return;

            _leaderboardShown = true;

            try
            {
                if (_sessionRecord.CompletedLevels > 0)
                    ScoreBoard.SaveOrUpdate(_sessionRecord);

                List<ScoreRecord> records =
                    ScoreBoard.LoadTopRecords(10);

                using (ScoreBoardForm board =
                    new ScoreBoardForm(records))
                {
                    board.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Khong the hien thi bang thanh tich:\n" + ex.Message,
                    "Loi Leaderboard",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void TimerTick(object sender, EventArgs e)
        {
            long now = _clock.ElapsedMilliseconds;
            int dt = (int)Math.Max(0, Math.Min(250, now - _lastTickMs));
            _lastTickMs = now;

            GameState previousState = _engine.State;
            _engine.Update(dt);
            GameState newState = _engine.State;

            if (previousState != newState)
                _gamePanel.CancelDrawing();

            // The engine sound plays only while the van is driving (not planning, paused or finished).
            AudioManager.SetEngineRunning(newState == GameState.Running);

            _gamePanel.Invalidate();
            RefreshInterface();

            if (previousState != newState &&
                (newState == GameState.Won || newState == GameState.Lost))
                HandleLevelFinished(newState);
        }

        private void HandleLevelFinished(GameState result)
        {
            // A WinForms timer keeps ticking while a modal dialog is open,
            // so stop the game loop until the dialogs are closed.
            _timer.Stop();
            try
            {
                if (result == GameState.Won) AudioManager.PlayWin();
                else if (_engine.LossWasCrash) AudioManager.PlayCrash();
                else AudioManager.PlayLose();

                Refresh();   // show the result panel behind the dialogs

                if (result == GameState.Won)
                {
                    SaveCurrentLevelResult();

                    if (_engine.CurrentLevelNumber >= MaxLevelNumber)
                        ShowLeaderboardAtEnd();
                }
                else
                {
                    ShowLeaderboardAtEnd();
                }
            }
            finally
            {
                _lastTickMs = _clock.ElapsedMilliseconds;
                if (!IsDisposed) _timer.Start();
            }
        }

        private void RefreshInterface()
        {
            _stateLabel.Text = "STATE  " + _engine.State.ToString().ToUpperInvariant();
            string phase = _engine.ActivePhase == GameState.Planning ? "PLAN" : "DELIVERY";
            _timeLabel.Text = phase + " TIME  " + _engine.RemainingSeconds + " s";
            _scoreLabel.Text = _sessionRecord.CompletedLevels > 0
                ? "TOTAL PATH  " + _sessionRecord.TotalPathUsed + " / " + _sessionRecord.TotalPathLimit
                : "TOTAL PATH  -";
            Text = "Delivery Driver — Level " + _engine.CurrentLevelNumber + ": " + _engine.Map.LevelName;
            _pathLabel.Text = "PATH  " + _engine.PathLength + " / " + _engine.InkLimit;

            _goButton.Enabled = _engine.CanGo;
            _pauseButton.Enabled =
                _engine.State == GameState.Planning || _engine.State == GameState.Running;//kiem tra xem da pause chua neu da pause roi thi khong cho pause nua
            _resumeButton.Enabled =
                _engine.State == GameState.Paused;
            _pauseButton.Visible = _engine.State != GameState.Paused;
            _resumeButton.Visible = _engine.State == GameState.Paused;
            _undoButton.Enabled = _engine.State == GameState.Planning && _engine.PathLength > 1;
            _clearButton.Enabled = _engine.State == GameState.Planning && _engine.PathLength > 0;
            _retryButton.Text = (_engine.State == GameState.Planning) ? "Restart route" : "Retry / Reset";

            bool hasWon = _engine.State == GameState.Won;
            bool hasLost = _engine.State == GameState.Lost;
            bool hasResult = hasWon || hasLost;

            _resultLabel.Visible = hasResult;

            if (hasWon)
            {
                _resultLabel.Text =
                    "VICTORY!\n\n" +
                    _engine.ResultMessage +
                    "\n\nPath used: " + _engine.LastLevelPathUsed + " / " + _engine.InkLimit +
                    "\nPlan time: " + _engine.LastLevelPlanSeconds + " s" +
                    "\nTotal path: " + _sessionRecord.TotalPathUsed + " / " + _sessionRecord.TotalPathLimit;
            }
            else if (hasLost)
            {
                _resultLabel.Text =
                    "DEFEAT!\n\n" +
                    _engine.ResultMessage +
                    "\n\nHay thu lai man nay!";
            }
            else
            {
                _resultLabel.Text = String.Empty;
            }

            _resultLabel.ForeColor = hasWon
                ? Color.FromArgb(32, 122, 77)
                : Color.FromArgb(171, 56, 48);
            _retryButton.Text = "Retry";
            _retryButton.Visible = hasResult;
            _retryButton.Enabled = hasResult;

            _nextLevelButton.Visible = hasWon;

            _nextLevelButton.Text =
                _engine.CurrentLevelNumber < MaxLevelNumber
                    ? "Next Level  ▶"
                    : "All Levels Completed";

            _nextLevelButton.Enabled =
                hasWon &&
                _engine.CurrentLevelNumber < MaxLevelNumber;
        }
    }
}