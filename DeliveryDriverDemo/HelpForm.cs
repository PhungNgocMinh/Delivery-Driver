using System.Drawing;
using System.Windows.Forms;

namespace DeliveryDriverDemo
{
    internal sealed class HelpForm : Form
    {
        public HelpForm()
        {
            Text = "Delivery Driver — Help";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(720, 590);
            MinimumSize = new Size(620, 500);
            BackColor = Color.FromArgb(239, 243, 246);
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
                Padding = new Padding(22),
                BackColor = BackColor
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
            Controls.Add(root);

            Label heading = new Label
            {
                Text = "HOW TO MAKE A DELIVERY",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 17f, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 39, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            root.Controls.Add(heading, 0, 0);

            RichTextBox instructions = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(54, 67, 78),
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                DetectUrls = true,
                Text =
                    "CONTROLS\n" +
                    "• Hold the left mouse button on S (the depot) and drag along connected roads.\n" +
                    "• Release the mouse to finish drawing. Press Go only after your route reaches F.\n" +
                    "• Drag back onto the previous tile to undo one tile. Use Undo last tile or Clear route for edits.\n" +
                    "• Press P (or the Pause / Resume button) to freeze everything: the countdowns, the traffic and the driver.\n\n" +
                    "ROUTE RULES\n" +
                    "• Each added tile must be directly above, below, left or right of the previous tile. Diagonal moves are not allowed.\n" +
                    "• Houses and grass are blocked. You cannot revisit a tile, except to undo the previous step.\n" +
                    "• The route cannot exceed the ink limit shown in the HUD. Go is enabled only when the route reaches F.\n\n" +
                    "TILE GUIDE AND SPEEDS\n" +
                    "• S — Start/depot. F — Finish/customer. R — Normal road. I — Intersection.\n" +
                    "• M — Mud: the driver takes about 1.5 seconds per move when entering this tile.\n" +
                    "• B — Highway: the driver takes about 0.6 seconds per move when entering this tile.\n" +
                    "• Other ordinary road tiles take about 1 second per move.\n\n" +
                    "TRAFFIC AND TIMING\n" +
                    "• Traffic follows predictable routes. Watch where vehicles travel before choosing a route.\n" +
                    "• The driver loses if it enters the same tile as a traffic vehicle or swaps cells with one during the same update.\n" +
                    "• While you plan, dashed arrows show each traffic vehicle's route. Traffic only starts moving when you press Go.\n" +
                    "• A longer route may avoid a crossing but consumes more time. Plan before pressing Go.\n\n" +
                    "TIMERS\n" +
                    "• PLAN TIME counts down while you draw your route. If it reaches zero the level is lost.\n" +
                    "• DELIVERY TIME starts when you press Go. Reach F before it reaches zero.\n\n" +
                    "WIN, LOSE, SCORE AND RETRY\n" +
                    "• Win by reaching F before DELIVERY TIME runs out.\n" +
                    "• Lose if a collision occurs or either countdown reaches zero.\n" +
                    "• Level score = 100 + 5 x plan seconds left + 10 x delivery seconds left. Scores add up across levels.\n" +
                    "• After a win you can enter your name; results are saved to the leaderboard (thanhtichs.txt).\n" +
                    "• Retry reloads the level with an empty route. Retrying after a win removes that level's score."
            };
            root.Controls.Add(instructions, 0, 1);

            FlowLayoutPanel footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft
            };
            Button closeButton = new Button
            {
                Text = "CLOSE",
                Width = 110,
                Height = 36,
                BackColor = Color.FromArgb(34, 143, 99),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                UseVisualStyleBackColor = false
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += delegate { Close(); };
            footer.Controls.Add(closeButton);
            root.Controls.Add(footer, 0, 2);
            AcceptButton = closeButton;
        }
    }
}