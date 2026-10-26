using System.Drawing;
using System.Windows.Forms;

namespace DeliveryDriverDemo
{
    /// <summary>
    /// About screen template. Replace the team and asset-credit placeholders with verified details
    /// before submitting the project; names, IDs, and licences are deliberately not fabricated.
    /// </summary>
    internal sealed class AboutForm : Form
    {
        public AboutForm()
        {
            Text = "Delivery Driver — About";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(650, 540);
            MinimumSize = new Size(600, 480);
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
                RowCount = 4,
                Padding = new Padding(22),
                BackColor = BackColor
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
            Controls.Add(root);

            Label heading = new Label
            {
                Text = "DELIVERY DRIVER",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 39, 50),
                TextAlign = ContentAlignment.MiddleLeft
            };
            root.Controls.Add(heading, 0, 0);

            Label description = new Label
            {
                Dock = DockStyle.Fill,
                Text = "A route-planning mini-game developed for the Programming on Windows module.\n" +
                       "Technology: C# Windows Forms Application • Target: .NET Framework 4.7.2\n" +
                       "The player plans a delivery route and must avoid timed crossing traffic.",
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Color.FromArgb(60, 73, 83),
                TextAlign = ContentAlignment.MiddleLeft
            };
            root.Controls.Add(description, 0, 1);

            TableLayoutPanel details = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White,
                Padding = new Padding(16)
            };
            details.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));
            details.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));

            GroupBox teamBox = new GroupBox
            {
                Text = "TEAM MEMBERS — REPLACE THE PLACEHOLDERS",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(28, 39, 50),
                Padding = new Padding(12)
            };
            Label team = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(60, 73, 83),
                Text =
                    "Member A — [Full name] | Student ID: [MSSV] | Class: [Lớp]\n" +
                    "Member B — [Full name] | Student ID: [MSSV] | Class: [Lớp]\n" +
                    "Member C — [Full name] | Student ID: [MSSV] | Class: [Lớp]\n" +
                    "Member D — [Full name] | Student ID: [MSSV] | Class: [Lớp]",
                TextAlign = ContentAlignment.MiddleLeft
            };
            teamBox.Controls.Add(team);

            GroupBox creditsBox = new GroupBox
            {
                Text = "ASSET CREDITS AND LICENCES",
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(28, 39, 50),
                Padding = new Padding(12)
            };
            Label credits = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(60, 73, 83),
                Text = "Current demo uses shapes drawn by the program. Before adding external assets, record each image, sound or music item with its title, creator/source URL, licence and any required attribution.\n\n[Add the team's verified asset credits here.]",
                TextAlign = ContentAlignment.MiddleLeft
            };
            creditsBox.Controls.Add(credits);

            details.Controls.Add(teamBox, 0, 0);
            details.Controls.Add(creditsBox, 0, 1);
            root.Controls.Add(details, 0, 2);

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
            root.Controls.Add(footer, 0, 3);
            AcceptButton = closeButton;
        }
    }
}
