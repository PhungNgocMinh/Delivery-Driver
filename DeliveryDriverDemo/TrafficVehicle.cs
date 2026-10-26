using System;
using System.Collections.Generic;
using System.Drawing;

namespace DeliveryDriverDemo
{
    internal sealed class TrafficVehicle
    {
        public string Name { get; private set; }
        public List<Point> Route { get; private set; }
        public bool IsLoop { get; private set; }
        public int StepIntervalMs { get; private set; }
        public int InitialDelayMs { get; private set; }
        public Color BodyColor { get; private set; }

        public int Index { get; private set; }
        public int Direction { get; private set; }
        public int ElapsedMs { get; private set; }
        public int DelayRemainingMs { get; private set; }
        public Point Cell { get; private set; }
        public Point PreviousCell { get; private set; }
        public Point Heading { get; private set; }
        public bool MovedThisTick { get; private set; }

        public TrafficVehicle(string name, IEnumerable<Point> route, bool isLoop,
            int stepIntervalMs, int initialDelayMs, Color bodyColor)
        {
            Name = name;
            Route = new List<Point>(route);
            IsLoop = isLoop;
            StepIntervalMs = stepIntervalMs;
            InitialDelayMs = initialDelayMs;
            BodyColor = bodyColor;
            Reset();
        }

        public void Reset()
        {
            Index = 0;
            Direction = 1;
            ElapsedMs = 0;
            DelayRemainingMs = InitialDelayMs;
            Cell = Route.Count == 0 ? Point.Empty : Route[0];
            PreviousCell = Cell;
            Heading = Route.Count > 1
                ? new Point(Math.Sign(Route[1].X - Route[0].X), Math.Sign(Route[1].Y - Route[0].Y))
                : new Point(1, 0);
            MovedThisTick = false;
        }

        public void Update(int dtMs)
        {
            PreviousCell = Cell;
            MovedThisTick = false;
            if (dtMs <= 0 || Route.Count < 2) return;

            // Consume the initial delay first, carrying any leftover part of this tick forward.
            if (DelayRemainingMs > 0)
            {
                int consumed = dtMs < DelayRemainingMs ? dtMs : DelayRemainingMs;
                DelayRemainingMs -= consumed;
                dtMs -= consumed;
            }
            if (dtMs <= 0) return;

            ElapsedMs += dtMs;
            while (ElapsedMs >= StepIntervalMs)
            {
                ElapsedMs -= StepIntervalMs;
                Advance();
                MovedThisTick = true;
            }
        }

        private void Advance()
        {
            if (IsLoop)
            {
                Index = (Index + 1) % Route.Count;
            }
            else
            {
                int next = Index + Direction;
                if (next >= Route.Count)
                {
                    Direction = -1;
                    next = Route.Count - 2;
                }
                else if (next < 0)
                {
                    Direction = 1;
                    next = 1;
                }
                Index = next;
            }
            Point old = Cell;
            Cell = Route[Index];
            Heading = new Point(Math.Sign(Cell.X - old.X), Math.Sign(Cell.Y - old.Y));
        }
    }
}
