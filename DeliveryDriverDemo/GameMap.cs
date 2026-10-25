using System;
using System.Drawing;

namespace DeliveryDriverDemo
{
    /// <summary>
    /// The one hardcoded demo map. Coordinates use (column, row), matching Point.X/Y.
    /// </summary>
    internal sealed class GameMap
    {
        public const int Columns = 16;
        public const int Rows = 10;
        public const int InkLimit = 32;
        public const int TimeLimitSeconds = 45;

        private readonly char[,] _tiles;

        public Point Start { get; private set; }
        public Point Finish { get; private set; }

        private GameMap(char[,] tiles, Point start, Point finish)
        {
            _tiles = tiles;
            Start = start;
            Finish = finish;
        }

        public char TileAt(Point cell)
        {
            if (!InBounds(cell)) return 'X';
            return _tiles[cell.Y, cell.X];
        }

        public bool InBounds(Point cell)
        {
            return cell.X >= 0 && cell.X < Columns && cell.Y >= 0 && cell.Y < Rows;
        }

        public bool IsDrivable(Point cell)
        {
            char tile = TileAt(cell);
            return tile == 'R' || tile == 'I' || tile == 'M' || tile == 'B' || tile == 'S' || tile == 'F';
        }

        public static GameMap CreateDemo()
        {
            char[,] tiles = new char[Rows, Columns];

            // Start with grass, then add blocks of houses for a little neighbourhood context.
            for (int row = 0; row < Rows; row++)
                for (int col = 0; col < Columns; col++)
                    tiles[row, col] = 'G';

            SetRectangle(tiles, 2, 2, 4, 3, 'H');
            SetRectangle(tiles, 7, 2, 9, 3, 'H');
            SetRectangle(tiles, 11, 2, 13, 3, 'H');
            SetRectangle(tiles, 3, 5, 5, 6, 'H');
            SetRectangle(tiles, 7, 5, 9, 6, 'H');
            SetRectangle(tiles, 11, 5, 12, 6, 'H');
            SetRectangle(tiles, 3, 8, 5, 9, 'H');
            SetRectangle(tiles, 8, 8, 11, 9, 'H');

            // Main road, a lower detour, and the vertical connectors between them.
            SetHorizontalRoad(tiles, 1, 1, 14);
            SetHorizontalRoad(tiles, 4, 1, 14);
            SetHorizontalRoad(tiles, 7, 2, 13);
            SetVerticalRoad(tiles, 1, 1, 4);
            SetVerticalRoad(tiles, 14, 1, 4);
            SetVerticalRoad(tiles, 6, 1, 7);
            SetVerticalRoad(tiles, 10, 1, 7);
            SetVerticalRoad(tiles, 2, 4, 7);
            SetVerticalRoad(tiles, 13, 4, 7);

            // Mark junctions, plus a slow mud tile and a fast highway tile.
            Point[] intersections =
            {
                new Point(2, 4), new Point(6, 1), new Point(10, 1), new Point(14, 4),
                new Point(6, 4), new Point(10, 4), new Point(2, 7), new Point(13, 7)
            };
            foreach (Point p in intersections) tiles[p.Y, p.X] = 'I';

            tiles[4, 4] = 'M';       // Mud: slower when the driver enters this cell.
            tiles[7, 11] = 'B';      // Highway: faster when the driver enters this cell.

            Point start = new Point(1, 1);
            Point finish = new Point(14, 1);
            tiles[start.Y, start.X] = 'S';
            tiles[finish.Y, finish.X] = 'F';

            return new GameMap(tiles, start, finish);
        }

        private static void SetRectangle(char[,] tiles, int left, int top, int right, int bottom, char value)
        {
            for (int row = top; row <= bottom; row++)
                for (int col = left; col <= right; col++)
                    tiles[row, col] = value;
        }

        private static void SetHorizontalRoad(char[,] tiles, int row, int fromCol, int toCol)
        {
            for (int col = fromCol; col <= toCol; col++) tiles[row, col] = 'R';
        }

        private static void SetVerticalRoad(char[,] tiles, int col, int fromRow, int toRow)
        {
            for (int row = fromRow; row <= toRow; row++) tiles[row, col] = 'R';
        }
    }
}
