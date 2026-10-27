using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliveryDriverDemo
{
    internal sealed class ScoreRecord
    {
        public string SessionId { get; set; }
        public string Name { get; set; }

        public int Level1Score { get; set; }
        public int Level1TimeSeconds { get; set; }

        public int Level2Score { get; set; }
        public int Level2TimeSeconds { get; set; }

        public int Level3Score { get; set; }
        public int Level3TimeSeconds { get; set; }

        public DateTime UpdatedAt { get; set; }

        public int TotalScore
        {
            get
            {
                return Level1Score
                     + Level2Score
                     + Level3Score;
            }
        }

        public int TotalTimeSeconds
        {
            get
            {
                return Level1TimeSeconds
                     + Level2TimeSeconds
                     + Level3TimeSeconds;
            }
        }

        public int CompletedLevels
        {
            get
            {
                int count = 0;

                if (Level1Score > 0) count++;
                if (Level2Score > 0) count++;
                if (Level3Score > 0) count++;

                return count;
            }
        }

        public void SetLevelResult(
            int levelNumber, int score, int timeSeconds)
        {
            switch (levelNumber)
            {
                case 1:
                    Level1Score = score;
                    Level1TimeSeconds = timeSeconds;
                    break;

                case 2:
                    Level2Score = score;
                    Level2TimeSeconds = timeSeconds;
                    break;

                case 3:
                    Level3Score = score;
                    Level3TimeSeconds = timeSeconds;
                    break;
            }

            UpdatedAt = DateTime.Now;
        }

        public void ClearLevelResult(int levelNumber)
        {
            switch (levelNumber)
            {
                case 1:
                    Level1Score = 0;
                    Level1TimeSeconds = 0;
                    break;

                case 2:
                    Level2Score = 0;
                    Level2TimeSeconds = 0;
                    break;

                case 3:
                    Level3Score = 0;
                    Level3TimeSeconds = 0;
                    break;
            }

            UpdatedAt = DateTime.Now;
        }
    }
}
