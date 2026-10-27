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
        Lost,
        Paused //them pause vao game
    }

    /// <summary>
    /// Contains game rules only; it does not depend on WinForms controls.
    /// </summary>
    internal sealed class GameEngine
    {
        private int _driverPathIndex;
        private int _driverElapsedMs;
        // private int _remainingMs; 
        // chia game ra lam 2 giai doan la len ke hoach de ve duong di va sau do la thoi gian xe chay
        private const int DefaultPlanningTimeSeconds = 180; 

        private int _planningRemainingMs;
        private int _deliveryRemainingMs;

        
        private GameState _stateBeforePause; 
        private GameState _lastActivePhase;

        private bool _levelScoreAwarded;
        private int _pathUsedAtLevelStart;
        private string _currentLevelFilePath;
        private bool _drawing;
        private bool _driverMovedThisTick;

        public GameMap Map { get; private set; }
        public List<Point> Path { get; private set; }
        public List<TrafficVehicle> Vehicles { get; private set; }
        public GameState State { get; private set; }
        public Point DriverCell { get; private set; }
        public Point DriverPreviousCell { get; private set; }
        public Point DriverHeading { get; private set; }   // D: direction the driver sprite faces
        public string ResultMessage { get; private set; }
        public int TotalPathUsed { get; private set; }
        public int LastLevelPathUsed { get; private set; }
        public int PlanningTimeLimitSeconds { get; private set; }
        public int CurrentLevelNumber { get; private set; }

        /// <summary>True when the last loss was a collision (not a timeout); used for the crash sound.</summary>
        public bool LossWasCrash { get; private set; }

        /// <summary>Plan time of the level that was just won (the tie-breaker on the leaderboard).</summary>
        public int LastLevelPlanSeconds { get; private set; }

        /// <summary>Seconds spent planning so far in this level (the planning clock stops at GO).</summary>
        public int PlanningUsedSeconds
        {
            get
            {
                int usedMs = Math.Max(0, PlanningTimeLimitSeconds * 1000 - _planningRemainingMs);
                return (int)Math.Round(usedMs / 1000.0);
            }
        }
        public int PlanningRemainingSeconds
        {
            get
            {
                return Math.Max(
                    0, (_planningRemainingMs + 999) / 1000);
            }
        }

        public int DeliveryRemainingSeconds
        {
            get
            {
                return Math.Max(
                    0, (_deliveryRemainingMs + 999) / 1000);
            }
        }
        public int InkLimit { get { return Map.LevelInkLimit; } }
        public int PathLength { get { return Path.Count; } }
        // D: which countdown is active (Planning or Running), even while Paused / after Won / Lost.
        public GameState ActivePhase
        {
            get
            {
                if (State == GameState.Paused)
                    return _stateBeforePause;
                if (State == GameState.Won || State == GameState.Lost)
                    return _lastActivePhase;
                return State;
            }
        }

        public int RemainingSeconds
        {
            get
            {
                return ActivePhase == GameState.Planning
                    ? PlanningRemainingSeconds
                    : DeliveryRemainingSeconds;
            }
        }

        public int ElapsedTimeSeconds //dung de luu thoi gian da su dung de vuot  qua man choi
        {
            get
            {
                int planningUsed =
                    PlanningTimeLimitSeconds * 1000
                    - _planningRemainingMs;

                int deliveryUsed =
                    Map.LevelTimeLimitSeconds * 1000
                    - _deliveryRemainingMs;

                return Math.Max(
                    0, planningUsed + deliveryUsed) / 1000;
            }
        }
        public bool CanGo
        {
            get { return State == GameState.Planning && Path.Count >= 2 && Path[Path.Count - 1] == Map.Finish; }
        }

        public GameEngine() : this(1)
        {
        }

        public GameEngine(int levelNumber)
        {
            TotalPathUsed = 0;
            LastLevelPathUsed = 0;

            LoadLevel(levelNumber);
        }

        public GameEngine(string levelFilePath)
        {
            TotalPathUsed = 0;
            LastLevelPathUsed = 0;

            LoadLevelFromFile(levelFilePath);
        }

        public void LoadLevel(int levelNumber)
        {
            if (levelNumber < 1)
                levelNumber = 1;

            CurrentLevelNumber = levelNumber;

            _currentLevelFilePath =
    LevelLoader.GetLevelPath(levelNumber);

            InitializeLoadedMap(
                LevelLoader.LoadFromFile(_currentLevelFilePath));

            _pathUsedAtLevelStart = TotalPathUsed;
        }

        public void LoadLevelFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException(
                    "Đường dẫn file màn chơi không hợp lệ.",
                    nameof(filePath));

            _currentLevelFilePath =
                System.IO.Path.IsPathRooted(filePath)
                    ? filePath
                    : System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        filePath);

            CurrentLevelNumber = 0;

            InitializeLoadedMap(
                LevelLoader.LoadFromFile(_currentLevelFilePath));

            _pathUsedAtLevelStart = TotalPathUsed;
        }

        private void InitializeLoadedMap(GameMap loadedMap)
        {
            Map = loadedMap ?? GameMap.CreateDemo();

            Path = new List<Point>();

            // Dùng danh sách bot được đọc từ file màn.
            Vehicles = Map.LevelVehicles
                ?? new List<TrafficVehicle>();

            State = GameState.Planning;
            _stateBeforePause = GameState.Planning;
            _lastActivePhase = GameState.Planning;

            PlanningTimeLimitSeconds =
                DefaultPlanningTimeSeconds;

            _planningRemainingMs =
                PlanningTimeLimitSeconds * 1000;

            _deliveryRemainingMs =
                Map.LevelTimeLimitSeconds * 1000;

            _driverPathIndex = 0;
            _driverElapsedMs = 0;

            _drawing = false;
            _driverMovedThisTick = false;
            _levelScoreAwarded = false;

            LastLevelPathUsed = 0;
            LastLevelPlanSeconds = 0;
            LossWasCrash = false;
            ResultMessage = String.Empty;

            ResetDriver();

            foreach (TrafficVehicle vehicle in Vehicles)
                vehicle.Reset();
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
            if (!CanGo)
                return;

            _drawing = false;
            State = GameState.Running;
            _lastActivePhase = GameState.Running;

            _driverPathIndex = 0;
            _driverElapsedMs = 0;

            // Bắt đầu đồng hồ giao hàng từ đầu.
            _deliveryRemainingMs =
                Map.LevelTimeLimitSeconds * 1000;

            ResetDriver();

            foreach (TrafficVehicle vehicle in Vehicles)
                vehicle.Reset();

            ResultMessage = String.Empty;
            LossWasCrash = false;
        }
        //them pause va resume vao game
        public void Pause()
        {
            if (State != GameState.Planning &&
                State != GameState.Running)
                return;

            _stateBeforePause = State;
            _drawing = false;
            State = GameState.Paused;
        }

        public void Resume()
        {
            if (State != GameState.Paused)
                return;

            State = _stateBeforePause;
        }
        //lam lai retry de no chi reset lai diem cua man hien tai va giu nguyen diem cua cac man da hoan thanh truoc do
        public void Retry()
        {
          
            TotalPathUsed = _pathUsedAtLevelStart;
            LastLevelPathUsed = 0;
            _levelScoreAwarded = false;

            GameMap loadedMap =
                LevelLoader.LoadFromFile(_currentLevelFilePath);

            InitializeLoadedMap(loadedMap);

            _pathUsedAtLevelStart = TotalPathUsed;
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
            if (dtMs <= 0)
                return;

            // PLANNING: chỉ đếm giờ, bot đứng yên.
            if (State == GameState.Planning)
            {
                _lastActivePhase = GameState.Planning;
                _planningRemainingMs -= dtMs;

                if (_planningRemainingMs <= 0)
                {
                    _planningRemainingMs = 0;
                    State = GameState.Lost;

                    ResultMessage =
                        "PLANNING TIME'S UP! " +
                        "You ran out of time to plan your route.";
                }

                return;
            }

            // Pause, Won và Lost không cập nhật gameplay.
            if (State != GameState.Running)
                return;

            _lastActivePhase = GameState.Running;

            DriverPreviousCell = DriverCell;
            _driverMovedThisTick = false;

            // Bot chỉ di chuyển khi Running.
            foreach (TrafficVehicle vehicle in Vehicles)
                vehicle.Update(dtMs);

            _driverElapsedMs += dtMs;

            while (_driverPathIndex < Path.Count - 1)
            {
                Point nextCell = Path[_driverPathIndex + 1];
                int stepTime = GetDriverStepTime(nextCell);

                if (_driverElapsedMs < stepTime)
                    break;

                _driverElapsedMs -= stepTime;

                DriverPreviousCell = DriverCell;
                _driverPathIndex++;
                DriverCell = Path[_driverPathIndex];
                DriverHeading = new Point(
                    Math.Sign(DriverCell.X - DriverPreviousCell.X),
                    Math.Sign(DriverCell.Y - DriverPreviousCell.Y));

                _driverMovedThisTick = true;
            }

            _deliveryRemainingMs -= dtMs;

            if (CheckDriverCollision())
                return;

            if (DriverCell == Map.Finish)
            {
                AwardLevelScore();

                State = GameState.Won;

                ResultMessage =
                    "DELIVERY COMPLETE! You reached the customer.";

                return;
            }

            if (_deliveryRemainingMs <= 0)
            {
                _deliveryRemainingMs = 0;
                State = GameState.Lost;

                ResultMessage =
                    "TIME'S UP! The delivery did not arrive in time.";
            }
        }

        private int GetDriverStepTime(Point enteringCell)
        {
            char tile = Map.TileAt(enteringCell);
            if (tile == 'M') return 1500;
            if (tile == 'B') return 600;
            return 1000;
        }
        
        // Ranking value of a finished level: how many road tiles the planned route used.
        // Lower is better. Ties are broken by the planning time (LastLevelPlanSeconds).
        private int CalculateLevelScore()
        {
            return Path.Count;
        }

        private void AwardLevelScore()
        {
            if (_levelScoreAwarded)
                return;

            LastLevelPathUsed = CalculateLevelScore();
            LastLevelPlanSeconds = PlanningUsedSeconds;
            TotalPathUsed += LastLevelPathUsed;

            _levelScoreAwarded = true;
        }
        private bool CheckDriverCollision()
        {
            foreach (TrafficVehicle vehicle in Vehicles)
            {
                // Same-cell collision.
                if (DriverCell == vehicle.Cell)
                {
                    State = GameState.Lost;
                    LossWasCrash = true;
                    ResultMessage = "CRASH! Your delivery vehicle collided with " + vehicle.Name + ".";
                    return true;
                }

                // Swap collision: both vehicles moved through each other's cells in one tick.
                if (_driverMovedThisTick && vehicle.MovedThisTick &&
                    DriverCell == vehicle.PreviousCell && DriverPreviousCell == vehicle.Cell)
                {
                    State = GameState.Lost;
                    LossWasCrash = true;
                    ResultMessage = "CRASH! You crossed paths with " + vehicle.Name + ".";
                    return true;
                }
            }
            return false;
        }
    }
}