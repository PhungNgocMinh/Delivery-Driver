using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace DeliveryDriverDemo
{
    internal sealed class ScoreBoardForm : Form
    {
        public ScoreBoardForm(List<ScoreRecord> records)
        {
            Text = "Delivery Driver - Leaderboard";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1120, 480);
            MinimumSize = new Size(900, 400);
            BackColor = Color.FromArgb(245, 247, 249);

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(12)
            };

            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 55));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Percent, 100));

            layout.RowStyles.Add(
                new RowStyle(SizeType.Absolute, 45));

            Label title = new Label
            {
                Text = "BANG THANH TICH",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 39, 50)
            };

            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                MultiSelect = false,
                SelectionMode =
                    DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode =
                    DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                EnableHeadersVisualStyles = false
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor =
                Color.FromArgb(28, 39, 50);

            grid.ColumnHeadersDefaultCellStyle.ForeColor =
                Color.White;

            grid.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 9.5f, FontStyle.Bold);

            grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = 54;

            grid.Columns.Add("Rank", "Hạng");
            grid.Columns.Add("Name", "Người chơi");
            grid.Columns.Add("L1", "Màn 1\nĐường đi / Thời gian lập kế hoạch");
            grid.Columns.Add("L2", "Màn 2\nĐường đi / Thời gian lập kế hoạch");
            grid.Columns.Add("L3", "Màn 3\nĐường đi / Thời gian lập kế hoạch");
            grid.Columns.Add("TotalPath", "Tổng đường đã dùng");
            grid.Columns.Add("TotalTime", "Tổng thời gian lập kế hoạch");

            for (int i = 0; i < records.Count; i++)
            {
                ScoreRecord r = records[i];

                grid.Rows.Add(
                    i + 1,
                    r.Name,
                    FormatLevel(r.Level1PathUsed, 1, r.Level1PlanSeconds),
                    FormatLevel(r.Level2PathUsed, 2, r.Level2PlanSeconds),
                    FormatLevel(r.Level3PathUsed, 3, r.Level3PlanSeconds),
                    r.TotalPathUsed + " / " + r.TotalPathLimit,
                    FormatTime(r.TotalPlanSeconds));
            }

            if (records.Count == 0)
            {
                grid.Rows.Add(
                    "-", "Chưa có thành tích",
                    "-", "-", "-", "-", "-");
            }

            Button closeButton = new Button
            {
                Text = "Đóng",
                Width = 100,
                Height = 32,
                BackColor = Color.FromArgb(49, 102, 155),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Right
            };

            closeButton.FlatAppearance.BorderSize = 0;

            Panel footer = new Panel
            {
                Dock = DockStyle.Fill
            };

            closeButton.Location = new Point(
                footer.ClientSize.Width - closeButton.Width,
                5);

            footer.Resize += delegate
            {
                closeButton.Location = new Point(
                    footer.ClientSize.Width - closeButton.Width,
                    5);
            };

            closeButton.Click += delegate { Close(); };

            // LUAT XEP HANG 
            Label rule = new Label
            {
                Text = "Xếp hạng: tổng đường đi ít nhất đứng #1. " +
                       "Nếu bằng nhau, thời gian lập kế hoạch ngắn hơn xếp trên.",
                Dock = DockStyle.Left,
                Width = 760,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(92, 104, 114),
                Font = new Font("Segoe UI", 9f, FontStyle.Italic)
            };
            footer.Controls.Add(rule);
            footer.Controls.Add(closeButton);
            AudioManager.AttachClickSounds(this);

            layout.Controls.Add(title, 0, 0);
            layout.Controls.Add(grid, 0, 1);
            layout.Controls.Add(footer, 0, 2);

            Controls.Add(layout);
        }

        private static string FormatLevel(int pathUsed, int levelNumber, int planSeconds)
        {
            if (pathUsed <= 0)
                return "-";

            return pathUsed + " / " + ScoreBoard.InkLimitFor(levelNumber)
                + "  |  " + FormatTime(planSeconds);
        }

        private static string FormatTime(int seconds)
        {
            return string.Format(
                "{0:00}:{1:00}",
                seconds / 60,
                seconds % 60);
        }
    }
}
