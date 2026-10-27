using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DeliveryDriverDemo
{
    internal static class LevelLoader
    {
        private static readonly Color[] VehiclePalette = new Color[]
        {
            Color.FromArgb(220, 68, 55),   // Đỏ cam
            Color.FromArgb(245, 166, 35),  // Vàng cam
            Color.FromArgb(142, 68, 173),  // Tím
            Color.FromArgb(41, 128, 185),  // Xanh dương
            Color.FromArgb(39, 174, 96)    // Xanh lá
        };

        // Nap man choi tu file level{n}.txt trong thu muc "levels"
        public static string GetLevelPath(int levelNumber)
        {
            string fileName = string.Format(
                "level{0}.txt", levelNumber);

            // Kiểm tra thư mục chạy game trước.
            string baseDirectory =
                AppDomain.CurrentDomain.BaseDirectory;

            DirectoryInfo current =
                new DirectoryInfo(baseDirectory);

            // Tìm thư mục Levels từ thư mục chạy game
            // rồi lần lượt kiểm tra các thư mục cha.
            while (current != null)
            {
                string candidate = Path.Combine(
                    current.FullName,
                    "Levels",
                    fileName);

                if (File.Exists(candidate))
                    return candidate;

                current = current.Parent;
            }

            // Không tìm thấy: trả về đường dẫn mặc định
            // để LoadFromFile hiển thị thông báo lỗi.
            return Path.Combine(
                baseDirectory,
                "Levels",
                fileName);
        }

        public static GameMap LoadLevel(int levelNumber)
        {
            string fullPath = GetLevelPath(levelNumber);
            return LoadFromFile(fullPath);
        }

        // Nap man choi tu file tuong ung voi duong dan day du
        public static GameMap LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("Không tìm thấy file màn chơi tại: " + filePath);
                }

                string[] rawLines = File.ReadAllLines(filePath);
                string levelName = Path.GetFileNameWithoutExtension(filePath);
                int timeLimit = GameMap.TimeLimitSeconds;
                int inkLimit = GameMap.InkLimit;

                List<string> mapLines = new List<string>();
                List<string> vehicleLines = new List<string>();
                string currentSection = "";

                // Tach cac phan trong file level theo section [INFO], [MAP], [VEHICLES]
                foreach (string raw in rawLines)
                {
                    string line = (raw ?? "").Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith("//"))
                        continue;

                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        currentSection = line.ToUpperInvariant();
                        continue;
                    }

                    if (currentSection == "[INFO]")
                    {
                        int eqIndex = line.IndexOf('=');
                        if (eqIndex > 0)
                        {
                            string key = line.Substring(0, eqIndex).Trim().ToLowerInvariant();
                            string val = line.Substring(eqIndex + 1).Trim();
                            int parsedInt;
                            if (key == "name") levelName = val;
                            else if (key == "time" && int.TryParse(val, out parsedInt) && parsedInt > 0) timeLimit = parsedInt;
                            else if (key == "ink" && int.TryParse(val, out parsedInt) && parsedInt > 0) inkLimit = parsedInt;
                        }
                    }
                    else if (currentSection == "[MAP]")
                    {
                        mapLines.Add(line.ToUpperInvariant());
                    }
                    else if (currentSection == "[VEHICLES]")
                    {
                        vehicleLines.Add(line);
                    }
                }

                // Kiem tra so luong dong cua ban do co dung voi so dong yeu cau
                if (mapLines.Count != GameMap.Rows)
                {
                    throw new InvalidDataException(
                        string.Format("Bản đồ trong file '{0}' phải có đúng {1} dòng (hiện có {2} dòng).",
                        Path.GetFileName(filePath), GameMap.Rows, mapLines.Count));
                }

                char[,] tiles = new char[GameMap.Rows, GameMap.Columns];
                Point? startPoint = null;
                Point? finishPoint = null;

                for (int r = 0; r < GameMap.Rows; r++)
                {
                    string rowText = mapLines[r];
                    if (rowText.Length != GameMap.Columns)
                    {
                        throw new InvalidDataException(
                            string.Format("Dòng {0} của bản đồ trong '{1}' phải có đúng {2} ký tự.",
                            r + 1, Path.GetFileName(filePath), GameMap.Columns));
                    }

                    for (int c = 0; c < GameMap.Columns; c++)
                    {
                        char ch = rowText[c];
                        if (ch == '.') ch = 'G'; // Cho phép ký tự '.' đại diện cho cỏ (Grass)

                        if (ch != 'G' && ch != 'H' && ch != 'R' && ch != 'I' &&
                            ch != 'M' && ch != 'B' && ch != 'S' && ch != 'F')
                        {
                            throw new InvalidDataException(
                                string.Format("Ký tự ô '{0}' tại (cột {1}, dòng {2}) không hợp lệ.", ch, c, r));
                        }

                        if (ch == 'S')
                        {
                            if (startPoint.HasValue)
                                throw new InvalidDataException("Bản đồ có nhiều hơn 1 điểm bắt đầu (S).");
                            startPoint = new Point(c, r);
                        }
                        else if (ch == 'F')
                        {
                            if (finishPoint.HasValue)
                                throw new InvalidDataException("Bản đồ có nhiều hơn 1 điểm đích (F).");
                            finishPoint = new Point(c, r);
                        }

                        tiles[r, c] = ch;
                    }
                }

                if (!startPoint.HasValue || !finishPoint.HasValue)
                {
                    throw new InvalidDataException("Bản đồ bắt buộc phải có đủ 1 điểm bắt đầu (S) và 1 điểm đích (F).");
                }

                // Tao ban do tam thoi de kiem tra luong xe giao thong
                GameMap tempMap = new GameMap(levelName, tiles, startPoint.Value, finishPoint.Value, inkLimit, timeLimit, null);

                // Doc va tao danh sach cac xe giao thong tu phan [VEHICLES]
                List<TrafficVehicle> vehicles = new List<TrafficVehicle>();
                for (int i = 0; i < vehicleLines.Count; i++)
                {
                    TrafficVehicle v = ParseVehicleLine(vehicleLines[i], i + 1, tempMap, VehiclePalette[i % VehiclePalette.Length]);
                    vehicles.Add(v);
                }

                return new GameMap(levelName, tiles, startPoint.Value, finishPoint.Value, inkLimit, timeLimit, vehicles);
            }
            catch (Exception ex)
            {
                // Bao loi va tra ve man choi mac dinh de khong lam treo chuong trinh
                MessageBox.Show(
                    "Không thể nạp file màn chơi. Hệ thống sẽ tự động chuyển về màn chơi mặc định.\n\nChi tiết lỗi: " + ex.Message,
                    "Cảnh báo nạp màn chơi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return CreateDefaultFallbackMap();
            }
        }

        private static TrafficVehicle ParseVehicleLine(string line, int index, GameMap map, Color color)
        {
            // Dinh dang cua dong cau hinh xe: Name;Mode;StepMs;DelayMs;Coord1,Coord2,Coord3,...
            string[] parts = line.Split(';');
            if (parts.Length < 5)
            {
                throw new InvalidDataException("Dòng cấu hình xe số " + index + " thiếu thông tin: " + line);
            }

            string name = parts[0].Trim();
            string mode = parts[1].Trim().ToLowerInvariant();
            bool isLoop = (mode == "loop");

            int stepMs;
            if (!int.TryParse(parts[2].Trim(), out stepMs) || stepMs < 100)
                stepMs = 900;

            int delayMs;
            if (!int.TryParse(parts[3].Trim(), out delayMs) || delayMs < 0)
                delayMs = 0;

            string[] coordTokens = parts[4].Trim().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            List<Point> route = new List<Point>();

            foreach (string token in coordTokens)
            {
                string[] xy = token.Split(',');
                int col, row;
                if (xy.Length != 2 || !int.TryParse(xy[0], out col) || !int.TryParse(xy[1], out row))
                {
                    throw new InvalidDataException("Tọa độ lộ trình xe '" + name + "' không hợp lệ: " + token);
                }

                Point cell = new Point(col, row);
                if (!map.IsDrivable(cell))
                {
                    throw new InvalidDataException(
                        string.Format("Xe '{0}' đi vào ô không phải đường tại ({1},{2}).", name, col, row));
                }

                if (route.Count > 0)
                {
                    Point prev = route[route.Count - 1];
                    int dist = Math.Abs(cell.X - prev.X) + Math.Abs(cell.Y - prev.Y);
                    if (dist != 1)
                    {
                        throw new InvalidDataException(
                            string.Format("Lộ trình xe '{0}' bị ngắt quãng giữa ({1},{2}) và ({3},{4}).",
                            name, prev.X, prev.Y, cell.X, cell.Y));
                    }
                }

                route.Add(cell);
            }

            if (route.Count < 2)
            {
                throw new InvalidDataException("Lộ trình của xe '" + name + "' phải có ít nhất 2 ô.");
            }

            return new TrafficVehicle(name, route, isLoop, stepMs, delayMs, color);
        }

        private static GameMap CreateDefaultFallbackMap()
        {
            GameMap demo = GameMap.CreateDemo();
            List<TrafficVehicle> defaultVehicles = new List<TrafficVehicle>
            {
                new TrafficVehicle(
                    "Loop Car",
                    new List<Point>
                    {
                        new Point(6, 1), new Point(7, 1), new Point(8, 1), new Point(9, 1), new Point(10, 1),
                        new Point(10, 2), new Point(10, 3), new Point(10, 4),
                        new Point(9, 4), new Point(8, 4), new Point(7, 4), new Point(6, 4),
                        new Point(6, 3), new Point(6, 2)
                    },
                    true, 850, 1600, Color.FromArgb(220, 68, 55)),
                new TrafficVehicle(
                    "Shuttle Van",
                    new List<Point>
                    {
                        new Point(6, 7), new Point(6, 6), new Point(6, 5), new Point(6, 4),
                        new Point(6, 3), new Point(6, 2), new Point(6, 1)
                    },
                    false, 1200, 800, Color.FromArgb(245, 166, 35))
            };

            char[,] tiles = new char[GameMap.Rows, GameMap.Columns];
            for (int r = 0; r < GameMap.Rows; r++)
            {
                for (int c = 0; c < GameMap.Columns; c++)
                {
                    tiles[r, c] = demo.TileAt(new Point(c, r));
                }
            }

            return new GameMap("Demo Fallback", tiles, demo.Start, demo.Finish, GameMap.InkLimit, GameMap.TimeLimitSeconds, defaultVehicles);
        }
    }
}