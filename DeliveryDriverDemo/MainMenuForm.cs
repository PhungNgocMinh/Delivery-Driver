using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DeliveryDriverDemo
{
    /// <summary>
    /// Entry screen for the game. The menu is intentionally built with ordinary WinForms
    /// controls so the team can easily restyle and extend it during integration.
    /// </summary>
    internal sealed class MainMenuForm : Form
    {
        private readonly Color _navy = Color.FromArgb(28, 39, 50);
        private readonly Color _green = Color.FromArgb(34, 143, 99);
        private readonly Color _page = Color.FromArgb(235, 239, 242);

        public MainMenuForm()
        {
            Text = "Delivery Driver";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(900, 650);
            MinimumSize = new Size(760, 560);
            BackColor = _page;
            Font = new Font("Segoe UI", 10f, FontStyle.Regular);

            BuildLayout();
        }

        private void BuildLayout()
        {
            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(26),
                BackColor = _page
            };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 42f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 48f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 10f));
            Controls.Add(root);

            Panel hero = new Panel { Dock = DockStyle.Fill, BackColor = _navy, Padding = new Padding(28) };
            Label title = new Label
            {
                Text = "DELIVERY\nDRIVER",
                Dock = DockStyle.Top,
                Height = 122,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 30f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Label subtitle = new Label
            {
                Text = "Plan your route. Watch the traffic. Deliver on time.",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(205, 222, 232),
                Font = new Font("Segoe UI", 12f, FontStyle.Regular),
                TextAlign = ContentAlignment.TopCenter
            };
            hero.Controls.Add(subtitle);
            hero.Controls.Add(title);
            root.Controls.Add(hero, 0, 0);

            FlowLayoutPanel menu = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(0, 18, 0, 0),
                BackColor = _page
            };
            menu.Controls.Add(CreateMenuButton("PLAY", _green, Color.White, PlayClicked));
            menu.Controls.Add(CreateMenuButton("LEADERBOARD", Color.FromArgb(218, 226, 231), _navy, LeaderboardClicked));
            menu.Controls.Add(CreateMenuButton("HELP", Color.FromArgb(218, 226, 231), _navy, HelpClicked));
            menu.Controls.Add(CreateMenuButton("ABOUT", Color.FromArgb(218, 226, 231), _navy, AboutClicked));
            menu.Controls.Add(CreateMenuButton("EXIT", Color.FromArgb(218, 226, 231), _navy, ExitClicked));
            root.Controls.Add(menu, 0, 1);

            Label footer = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Programming on Windows • C# Windows Forms • .NET Framework 4.7.2",
                ForeColor = Color.FromArgb(92, 104, 114),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular)
            };
            root.Controls.Add(footer, 0, 2);
        }

        private Button CreateMenuButton(string text, Color background, Color foreground, EventHandler handler)
        {
            Button button = new Button
            {
                Text = text,
                Width = 330,
                Height = 43,
                BackColor = background,
                ForeColor = foreground,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 9),
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += handler;
            return button;
        }

        private void PlayClicked(object sender, EventArgs e)
        {
            using (LevelSelectionForm selection = new LevelSelectionForm())
            {
                if (selection.ShowDialog(this) != DialogResult.OK)
                    return;

                // One GameForm hosts the whole session: "Next Level" loads the following level
                // inside it, so the running total score and leaderboard record stay together.
                using (GameForm game = new GameForm(selection.SelectedLevelIndex + 1))
                    game.ShowDialog(this);
            }
        }

        private void LeaderboardClicked(object sender, EventArgs e)
        {
            try
            {
                List<ScoreRecord> records = ScoreBoard.LoadTopRecords(10);
                using (ScoreBoardForm board = new ScoreBoardForm(records))
                    board.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Could not open the leaderboard:\n" + ex.Message,
                    "Leaderboard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void HelpClicked(object sender, EventArgs e)
        {
            using (HelpForm form = new HelpForm())
                form.ShowDialog(this);
        }

        private void AboutClicked(object sender, EventArgs e)
        {
            using (AboutForm form = new AboutForm())
                form.ShowDialog(this);
        }

        private void ExitClicked(object sender, EventArgs e)
        {
            Close();
        }
    }
}