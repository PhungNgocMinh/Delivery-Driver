using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DeliveryDriverDemo
{
    /// <summary>
    /// Double-buffered custom surface for the map, path, driver, and traffic.
    /// </summary>
    internal sealed class GamePanel : Panel
    {
        private readonly GameEngine _engine;
        private int _cellSize = 1;
        private int _offsetX;
        private int _offsetY;
        private bool _isDrawing;

        public GamePanel(GameEngine engine)
        {
            _engine = engine;
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.FromArgb(41, 51, 61);
            Cursor = Cursors.Cross;
            Margin = Padding.Empty;

            MouseDown += HandleMouseDown;
            MouseMove += HandleMouseMove;
            MouseUp += HandleMouseUp;
            UpdateGeometry();
        }
        private void DrawTrafficRoutes(Graphics g)
        {
            // Chỉ hiện đường xe bot khi người chơi đang vẽ đường.
            if (_engine.ActivePhase != GameState.Planning)
                return;

            Color[] routeColors =
            {
        Color.FromArgb(210, 255, 165, 0),  // Cam
        Color.FromArgb(210, 0, 180, 255),  // Xanh dương
        Color.FromArgb(210, 220, 60, 100)  // Đỏ hồng
    };

            // Thay _engine.Map bằng cách lấy GameMap
            // mà GamePanel hiện đang sử dụng để vẽ bản đồ.
            var vehicles = _engine.Map.LevelVehicles;

            int vehicleIndex = 0;

            foreach (TrafficVehicle vehicle in vehicles)
            {
                if (vehicle.Route == null || vehicle.Route.Count < 2)
                    continue;

                Color color = routeColors[vehicleIndex % routeColors.Length];

                using (Pen pen = new Pen(color, 3f))
                using (AdjustableArrowCap arrow = new AdjustableArrowCap(4, 5, true))
                {
                    pen.DashStyle = DashStyle.Dash;
                    pen.CustomEndCap = arrow;

                    int segments = vehicle.IsLoop ? vehicle.Route.Count : vehicle.Route.Count - 1;
                    for (int i = 0; i < segments; i++)
                    {
                        PointF start = CellCenter(vehicle.Route[i]);
                        PointF end = CellCenter(vehicle.Route[(i + 1) % vehicle.Route.Count]);

                        g.DrawLine(pen, start, end);
                    }
                }

                vehicleIndex++;
            }
        }
        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            UpdateGeometry();
            Invalidate();
        }

        public void CancelDrawing()
        {
            _isDrawing = false;
            _engine.EndDrawing();
        }

        private void HandleMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _engine.State != GameState.Planning) return;
            _isDrawing = _engine.BeginDrawing(ScreenToCell(e.Location));
            if (_isDrawing) Capture = true;
            Invalidate();
        }

        private void HandleMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDrawing) return;
            if (_engine.ExtendPath(ScreenToCell(e.Location))) Invalidate();
        }

        private void HandleMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _isDrawing = false;
            Capture = false;
            _engine.EndDrawing();
            Invalidate();
        }

        private void UpdateGeometry()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
            {
                _cellSize = 1;
                _offsetX = 0;
                _offsetY = 0;
                return;
            }

            _cellSize = Math.Max(1, Math.Min(ClientSize.Width / GameMap.Columns, ClientSize.Height / GameMap.Rows));
            _offsetX = (ClientSize.Width - _cellSize * GameMap.Columns) / 2;
            _offsetY = (ClientSize.Height - _cellSize * GameMap.Rows) / 2;
        }

        private Point ScreenToCell(Point screenPoint)
        {
            if (_cellSize <= 0 || screenPoint.X < _offsetX || screenPoint.Y < _offsetY)
                return new Point(-1, -1);

            int col = (screenPoint.X - _offsetX) / _cellSize;
            int row = (screenPoint.Y - _offsetY) / _cellSize;
            Point cell = new Point(col, row);
            if (!_engine.Map.InBounds(cell)) return new Point(-1, -1);
            return cell;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);
            
            DrawMap(g);
            DrawTrafficRoutes(g);
            DrawPath(g);

            foreach (TrafficVehicle vehicle in _engine.Vehicles)
                DrawVehicle(g, vehicle.Cell, vehicle.Heading, vehicle.BodyColor, false);

            DrawVehicle(g, _engine.DriverCell, _engine.DriverHeading,
                Color.FromArgb(49, 151, 232), true);

            using (Font font = new Font("Segoe UI", Math.Max(8f, _cellSize * 0.14f), FontStyle.Bold))
            using (Brush brush = new SolidBrush(Color.FromArgb(235, 245, 248)))
            {
                string legend = "DRAG FROM S TO F  •  Blue = your route  •  Dashed arrows = bot routes";
                SizeF size = g.MeasureString(legend, font);
                RectangleF backing = new RectangleF(12, 12, size.Width + 18, size.Height + 12);
                using (Brush panelBrush = new SolidBrush(Color.FromArgb(190, 26, 35, 45)))
                    g.FillRectangle(panelBrush, backing);
                g.DrawString(legend, font, brush, 21, 18);
            }
        }

        private void DrawMap(Graphics g)
        {
            for (int row = 0; row < GameMap.Rows; row++)
            {
                for (int col = 0; col < GameMap.Columns; col++)
                {
                    char tile = _engine.Map.TileAt(new Point(col, row));
                    Rectangle rect = CellRectangle(new Point(col, row));
                    Color fill;
                    switch (tile)
                    {
                        case 'H': fill = Color.FromArgb(193, 171, 148); break;
                        case 'R': fill = Color.FromArgb(78, 88, 99); break;
                        case 'I': fill = Color.FromArgb(67, 77, 88); break;
                        case 'M': fill = Color.FromArgb(130, 94, 61); break;
                        case 'B': fill = Color.FromArgb(113, 133, 146); break;
                        case 'S': fill = Color.FromArgb(32, 124, 173); break;
                        case 'F': fill = Color.FromArgb(42, 139, 105); break;
                        default: fill = Color.FromArgb(78, 119, 76); break;
                    }

                    using (Brush tileBrush = new SolidBrush(fill)) g.FillRectangle(tileBrush, rect);

                    if (tile == 'H') DrawHouse(g, rect);
                    else if (tile == 'I') DrawIntersection(g, rect);
                    else if (tile == 'M') DrawMud(g, rect);
                    else if (tile == 'B') DrawHighway(g, rect);
                    else if (tile == 'S') DrawTileLabel(g, rect, "S", Color.White);
                    else if (tile == 'F') DrawTileLabel(g, rect, "F", Color.White);

                    using (Pen gridPen = new Pen(Color.FromArgb(90, 26, 36, 43), 1f))
                        g.DrawRectangle(gridPen, rect.X, rect.Y, rect.Width, rect.Height);
                }
            }
        }

        private void DrawHouse(Graphics g, Rectangle rect)
        {
            Rectangle body = Rectangle.Inflate(rect, -_cellSize / 6, -_cellSize / 5);
            body.Y += _cellSize / 10;
            using (Brush bodyBrush = new SolidBrush(Color.FromArgb(224, 207, 177)))
                g.FillRectangle(bodyBrush, body);
            Point[] roof =
            {
                new Point(body.Left - _cellSize / 15, body.Top + _cellSize / 8),
                new Point(body.Left + body.Width / 2, body.Top - _cellSize / 8),
                new Point(body.Right + _cellSize / 15, body.Top + _cellSize / 8)
            };
            using (Brush roofBrush = new SolidBrush(Color.FromArgb(135, 77, 64))) g.FillPolygon(roofBrush, roof);
            Rectangle door = new Rectangle(body.Left + body.Width / 2 - Math.Max(2, _cellSize / 12),
                body.Bottom - Math.Max(4, _cellSize / 4), Math.Max(4, _cellSize / 6), Math.Max(4, _cellSize / 4));
            using (Brush doorBrush = new SolidBrush(Color.FromArgb(103, 73, 56))) g.FillRectangle(doorBrush, door);
        }

        private void DrawIntersection(Graphics g, Rectangle rect)
        {
            using (Pen stripe = new Pen(Color.FromArgb(180, 225, 205, 122), Math.Max(1f, _cellSize * 0.035f)))
            {
                int inset = Math.Max(4, _cellSize / 6);
                int gap = Math.Max(3, _cellSize / 12);
                for (int i = -1; i <= 1; i++)
                {
                    int y = rect.Top + rect.Height / 2 + i * gap * 2;
                    g.DrawLine(stripe, rect.Left + inset, y, rect.Left + _cellSize / 3, y);
                    g.DrawLine(stripe, rect.Right - _cellSize / 3, y, rect.Right - inset, y);
                }
            }
        }

        private void DrawMud(Graphics g, Rectangle rect)
        {
            using (Pen marks = new Pen(Color.FromArgb(170, 224, 187, 129), Math.Max(1f, _cellSize * 0.025f)))
            {
                g.DrawLine(marks, rect.Left + rect.Width / 4, rect.Top + rect.Height / 3,
                    rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
                g.DrawLine(marks, rect.Left + rect.Width / 2, rect.Top + rect.Height * 2 / 3,
                    rect.Left + rect.Width * 3 / 4, rect.Top + rect.Height / 2);
            }
        }

        private void DrawHighway(Graphics g, Rectangle rect)
        {
            using (Pen arrowPen = new Pen(Color.FromArgb(230, 237, 244, 246), Math.Max(1f, _cellSize * 0.035f)))
            {
                float cx = rect.Left + rect.Width / 2f;
                float cy = rect.Top + rect.Height / 2f;
                g.DrawLine(arrowPen, cx - _cellSize * 0.20f, cy, cx + _cellSize * 0.20f, cy);
                g.DrawLine(arrowPen, cx + _cellSize * 0.20f, cy, cx + _cellSize * 0.09f, cy - _cellSize * 0.10f);
                g.DrawLine(arrowPen, cx + _cellSize * 0.20f, cy, cx + _cellSize * 0.09f, cy + _cellSize * 0.10f);
            }
        }

        private void DrawTileLabel(Graphics g, Rectangle rect, string label, Color color)
        {
            using (Font font = new Font("Segoe UI", Math.Max(9f, _cellSize * 0.28f), FontStyle.Bold))
            using (Brush brush = new SolidBrush(color))
            {
                SizeF size = g.MeasureString(label, font);
                g.DrawString(label, font, brush, rect.Left + (rect.Width - size.Width) / 2,
                    rect.Top + (rect.Height - size.Height) / 2);
            }
        }

        private void DrawPath(Graphics g)
        {
            if (_engine.Path.Count == 0) return;

            if (_engine.Path.Count >= 2)
            {
                Point[] centers = new Point[_engine.Path.Count];
                for (int i = 0; i < _engine.Path.Count; i++) centers[i] = CellCenter(_engine.Path[i]);

                using (Pen pathPen = new Pen(Color.FromArgb(185, 46, 190, 255), Math.Max(4f, _cellSize * 0.11f)))
                {
                    pathPen.LineJoin = LineJoin.Round;
                    try { pathPen.CustomEndCap = new AdjustableArrowCap(Math.Max(3f, _cellSize * 0.10f), Math.Max(4f, _cellSize * 0.12f)); }
                    catch (ArgumentException) { /* The path is still readable without an arrowhead. */ }
                    g.DrawLines(pathPen, centers);
                }
            }

            using (Brush startBrush = new SolidBrush(Color.FromArgb(225, 154, 226, 255)))
            using (Pen outline = new Pen(Color.FromArgb(240, 19, 92, 142), Math.Max(1f, _cellSize * 0.025f)))
            {
                foreach (Point cell in _engine.Path)
                {
                    Point center = CellCenter(cell);
                    int radius = Math.Max(3, _cellSize / 10);
                    Rectangle dot = new Rectangle(center.X - radius, center.Y - radius, radius * 2, radius * 2);
                    g.FillEllipse(startBrush, dot);
                    g.DrawEllipse(outline, dot);
                }
            }
        }

        private void DrawVehicle(Graphics g, Point cell, Point heading, Color color, bool isDriver)
        {
            Point center = CellCenter(cell);
            float angle = 0f;
            if (heading.X < 0) angle = 180f;
            else if (heading.Y > 0) angle = 90f;
            else if (heading.Y < 0) angle = 270f;
            else angle = 0f;

            GraphicsState state = g.Save();
            g.TranslateTransform(center.X, center.Y);
            g.RotateTransform(angle);

            float bodyWidth = _cellSize * (isDriver ? 0.56f : 0.50f);
            float bodyHeight = _cellSize * (isDriver ? 0.35f : 0.30f);
            RectangleF body = new RectangleF(-bodyWidth / 2f, -bodyHeight / 2f, bodyWidth, bodyHeight);
            using (Brush bodyBrush = new SolidBrush(color)) g.FillRectangle(bodyBrush, body);
            using (Pen bodyPen = new Pen(Color.FromArgb(245, 25, 32, 40), Math.Max(1f, _cellSize * 0.025f)))
                g.DrawRectangle(bodyPen, body.X, body.Y, body.Width, body.Height);

            RectangleF windshield = new RectangleF(body.X + bodyWidth * 0.58f, body.Y + bodyHeight * 0.16f,
                bodyWidth * 0.19f, bodyHeight * 0.68f);
            using (Brush glassBrush = new SolidBrush(Color.FromArgb(225, 195, 231, 242)))
                g.FillRectangle(glassBrush, windshield);

            float wheelW = Math.Max(2f, _cellSize * 0.065f);
            float wheelH = Math.Max(3f, _cellSize * 0.12f);
            using (Brush wheelBrush = new SolidBrush(Color.FromArgb(28, 31, 35)))
            {
                g.FillRectangle(wheelBrush, body.X + bodyWidth * 0.18f, body.Y - wheelH * 0.45f, wheelW, wheelH);
                g.FillRectangle(wheelBrush, body.X + bodyWidth * 0.18f, body.Bottom - wheelH * 0.55f, wheelW, wheelH);
                g.FillRectangle(wheelBrush, body.Right - bodyWidth * 0.25f, body.Y - wheelH * 0.45f, wheelW, wheelH);
                g.FillRectangle(wheelBrush, body.Right - bodyWidth * 0.25f, body.Bottom - wheelH * 0.55f, wheelW, wheelH);
            }

            if (isDriver)
            {
                using (Font font = new Font("Segoe UI", Math.Max(6f, _cellSize * 0.11f), FontStyle.Bold))
                using (Brush textBrush = new SolidBrush(Color.White))
                    g.DrawString("YOU", font, textBrush, -bodyWidth * 0.42f, -bodyHeight * 0.42f);
            }

            g.Restore(state);
        }

        private Rectangle CellRectangle(Point cell)
        {
            return new Rectangle(_offsetX + cell.X * _cellSize, _offsetY + cell.Y * _cellSize, _cellSize, _cellSize);
        }

        private Point CellCenter(Point cell)
        {
            return new Point(_offsetX + cell.X * _cellSize + _cellSize / 2,
                _offsetY + cell.Y * _cellSize + _cellSize / 2);
        }
    }
}