using System;
using System.Collections.Generic;
using System.Drawing;

namespace DeliveryDriverDemo
{
    internal enum GameState
    {
        Planning,
        Running,
        Won,
        Lost
    }

    /// <summary>
    /// Contains game rules only; it does not depend on WinForms controls.
    /// </summary>
    internal sealed class GameEngine
    {
        private int _driverPathIndex;
        private int _driverElapsedMs;
        private int _remainingMs;
        private bool _drawing;
        private bool _driverMovedThisTick;

        public GameMap Map { get; private set; }
        public List<Point> Path { get; private set; }
        public List<TrafficVehicle> Vehicles { get; private set; }
        public GameState State { get; private set; }
        public Point DriverCell { get; private set; }
        public Point DriverPreviousCell { get; private set; }
        public Point DriverHeading { get; private set; }
        public string ResultMessage { get; private set; }

        public int InkLimit { get { return GameMap.InkLimit; } }
        public int PathLength { get { return Path.Count; } }
        public int RemainingSeconds
        {
            get
            {
                if (State == GameState.Planning) return GameMap.TimeLimitSeconds;
                return Math.Max(0, (_remainingMs + 999) / 1000);
            }
        }
        public bool CanGo
        {
            get { return State == GameState.Planning && Path.Count >= 2 && Path[Path.Count - 1] == Map.Finish; }
        }

        public GameEngine()
        {
            Map = GameMap.CreateDemo();
            Path = new List<Point>();
            Vehicles = CreateVehicles();
            State = GameState.Planning;
            ResetDriver();
            _remainingMs = GameMap.TimeLimitSeconds * 1000;
        }

        private List<TrafficVehicle> CreateVehicles()
        {
            // Vehicle 1 loops around the central intersection block.
            List<Point> loopRoute = new List<Point>
            {
                new Point(6, 4), new Point(7, 4), new Point(8, 4), new Point(9, 4), new Point(10, 4),
                new Point(10, 3), new Point(10, 2), new Point(10, 1), new Point(9, 1), new Point(8, 1),
                new Point(7, 1), new Point(6, 1), new Point(6, 2), new Point(6, 3)
            };

            // Vehicle 2 shuttles along the central road, reversing at each end.
            List<Point> shuttleRoute = new List<Point>();
            for (int col = 2; col <= 13; col++) shuttleRoute.Add(new Point(col, 4));

            return new List<TrafficVehicle>
            {
                new TrafficVehicle("Loop car", loopRoute, true, 850, 1600, Color.FromArgb(226, 91, 73)),
                new TrafficVehicle("Shuttle van", shuttleRoute, false, 1200, 800, Color.FromArgb(151, 91, 201))
            };
        }

        public bool BeginDrawing(Point cell)
        {
            if (State != GameState.Planning) return false;

            if (Path.Count == 0 && cell == Map.Start)
            {
                Path.Add(cell);
                _drawing = true;
                return true;
            }

            if (Path.Count > 0 && cell == Path[Path.Count - 1])
            {
                _drawing = true;
                return true;
            }

            return false;
        }

        public bool ExtendPath(Point cell)
        {
            if (!_drawing || State != GameState.Planning || Path.Count == 0 || !Map.InBounds(cell)) return false;

            Point last = Path[Path.Count - 1];
            if (cell == last) return false;

            // Dragging onto the previous cell removes the last path tile.
            if (Path.Count >= 2 && cell == Path[Path.Count - 2])
            {
                Path.RemoveAt(Path.Count - 1);
                return true;
            }

            int distance = Math.Abs(cell.X - last.X) + Math.Abs(cell.Y - last.Y);
            if (distance != 1 || !Map.IsDrivable(cell) || Path.Contains(cell) || Path.Count >= InkLimit)
                return false;

            Path.Add(cell);
            return true;
        }

        public void EndDrawing()
        {
            _drawing = false;
        }

        public void ClearPath()
        {
            if (State != GameState.Planning) return;
            Path.Clear();
            _drawing = false;
        }

        public void UndoPath()
        {
            if (State != GameState.Planning || Path.Count <= 1) return;
            Path.RemoveAt(Path.Count - 1);
            _drawing = false;
        }

        public void Go()
        {
            if (!CanGo) return;

            _drawing = false;
            State = GameState.Running;
            _driverPathIndex = 0;
            _driverElapsedMs = 0;
            _remainingMs = GameMap.TimeLimitSeconds * 1000;
            ResetDriver();
            foreach (TrafficVehicle vehicle in Vehicles) vehicle.Reset();
            ResultMessage = String.Empty;
        }

        public void Retry()
        {
            Path.Clear();
            _drawing = false;
            State = GameState.Planning;
            _driverPathIndex = 0;
            _driverElapsedMs = 0;
            _remainingMs = GameMap.TimeLimitSeconds * 1000;
            ResultMessage = String.Empty;
            ResetDriver();
            foreach (TrafficVehicle vehicle in Vehicles) vehicle.Reset();
        }

        private void ResetDriver()
        {
            DriverCell = Map.Start;
            DriverPreviousCell = DriverCell;
            DriverHeading = new Point(1, 0);
            _driverMovedThisTick = false;
        }

        public void Update(int dtMs)
        {
            if (State != GameState.Running || dtMs <= 0) return;

            DriverPreviousCell = DriverCell;
            _driverMovedThisTick = false;
            foreach (TrafficVehicle vehicle in Vehicles) vehicle.Update(dtMs);

            _driverElapsedMs += dtMs;
            while (_driverPathIndex < Path.Count - 1)
            {
                Point nextCell = Path[_driverPathIndex + 1];
                int stepTime = GetDriverStepTime(nextCell);
                if (_driverElapsedMs < stepTime) break;

                _driverElapsedMs -= stepTime;
                DriverPreviousCell = DriverCell;
                _driverPathIndex++;
                DriverCell = Path[_driverPathIndex];
                DriverHeading = new Point(
                    Math.Sign(DriverCell.X - DriverPreviousCell.X),
                    Math.Sign(DriverCell.Y - DriverPreviousCell.Y));
                _driverMovedThisTick = true;
            }

            _remainingMs -= dtMs;

            if (CheckDriverCollision()) return;
            if (DriverCell == Map.Finish)
            {
                State = GameState.Won;
                ResultMessage = "DELIVERY COMPLETE! You reached the customer.";
                return;
            }
            if (_remainingMs <= 0)
            {
                State = GameState.Lost;
                ResultMessage = "TIME'S UP! The delivery did not arrive in time.";
            }
        }

        private int GetDriverStepTime(Point enteringCell)
        {
            char tile = Map.TileAt(enteringCell);
            if (tile == 'M') return 1500;
            if (tile == 'B') return 600;
            return 1000;
        }

        private bool CheckDriverCollision()
        {
            foreach (TrafficVehicle vehicle in Vehicles)
            {
                // Same-cell collision.
                if (DriverCell == vehicle.Cell)
                {
                    State = GameState.Lost;
                    ResultMessage = "CRASH! Your delivery vehicle collided with " + vehicle.Name + ".";
                    return true;
                }

                // Swap collision: both vehicles moved through each other's cells in one tick.
                if (_driverMovedThisTick && vehicle.MovedThisTick &&
                    DriverCell == vehicle.PreviousCell && DriverPreviousCell == vehicle.Cell)
                {
                    State = GameState.Lost;
                    ResultMessage = "CRASH! You crossed paths with " + vehicle.Name + ".";
                    return true;
                }
            }
            return false;
        }
    }
}
