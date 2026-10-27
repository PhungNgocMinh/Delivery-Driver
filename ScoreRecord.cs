using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliveryDriverDemo
{
    /// SAP XEP BXH THEO MOI LUOT CHOI
    internal sealed class ScoreRecord
    {
        public string SessionId { get; set; }
        public string Name { get; set; }

        public int Level1PathUsed { get; set; }
        public int Level1PlanSeconds { get; set; }

        public int Level2PathUsed { get; set; }
        public int Level2PlanSeconds { get; set; }

        public int Level3PathUsed { get; set; }
        public int Level3PlanSeconds { get; set; }

        public DateTime UpdatedAt { get; set; }

        public int TotalPathUsed
        {
            get
            {
                return Level1PathUsed
                     + Level2PathUsed
                     + Level3PathUsed;
            }
        }

        /// check dl cho bxh
        public int TotalPathLimit
        {
            get
            {
                int total = 0;
                if (Level1PathUsed > 0) total += ScoreBoard.InkLimitFor(1);
                if (Level2PathUsed > 0) total += ScoreBoard.InkLimitFor(2);
                if (Level3PathUsed > 0) total += ScoreBoard.InkLimitFor(3);
                return total;
            }
        }

        public int TotalPlanSeconds
        {
            get
            {
                return Level1PlanSeconds
                     + Level2PlanSeconds
                     + Level3PlanSeconds;
            }
        }

        public int CompletedLevels
        {
            get
            {
                int count = 0;

                if (Level1PathUsed > 0) count++;
                if (Level2PathUsed > 0) count++;
                if (Level3PathUsed > 0) count++;

                return count;
            }
        }

        public void SetLevelResult(
            int levelNumber, int score, int timeSeconds)
        {
            switch (levelNumber)
            {
                case 1:
                    Level1PathUsed = score;
                    Level1PlanSeconds = timeSeconds;
                    break;

                case 2:
                    Level2PathUsed = score;
                    Level2PlanSeconds = timeSeconds;
                    break;

                case 3:
                    Level3PathUsed = score;
                    Level3PlanSeconds = timeSeconds;
                    break;
            }

            UpdatedAt = DateTime.Now;
        }

        public void ClearLevelResult(int levelNumber)
        {
            switch (levelNumber)
            {
                case 1:
                    Level1PathUsed = 0;
                    Level1PlanSeconds = 0;
                    break;

                case 2:
                    Level2PathUsed = 0;
                    Level2PlanSeconds = 0;
                    break;

                case 3:
                    Level3PathUsed = 0;
                    Level3PlanSeconds = 0;
                    break;
            }

            UpdatedAt = DateTime.Now;
        }
    }
}
