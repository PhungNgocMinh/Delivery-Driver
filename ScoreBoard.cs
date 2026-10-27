using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace DeliveryDriverDemo
{
    /// <summary> Moi lan choi = 1 BXH moi
    internal static class ScoreBoard
    {
        private const int MaxStoredRecords = 200;

        private static readonly Dictionary<int, int> InkLimits = new Dictionary<int, int>();

        public static readonly string FilePath =
            Path.Combine(AppData.Folder, "leaderboard.txt");

        private const string Header =
            "SessionId|Name|L1Path|L1PlanTime|" +
            "L2Path|L2PlanTime|L3Path|L3PlanTime|" +
            "TotalPath|TotalPlanTime|CompletedLevels|UpdatedAt";

        public static int InkLimitFor(int levelNumber)
        {
            int limit;
            if (InkLimits.TryGetValue(levelNumber, out limit))
                return limit;

            try
            {
                limit = LevelLoader.LoadLevel(levelNumber).LevelInkLimit;
            }
            catch (Exception)
            {
                limit = GameMap.InkLimit;
            }

            InkLimits[levelNumber] = limit;
            return limit;
        }

        public static int Compare(ScoreRecord a, ScoreRecord b)
        {
            // Choi nhieu man = rank cao hon
            int result = b.CompletedLevels.CompareTo(a.CompletedLevels);
            if (result != 0) return result;

            // SL nuoc di < 1st -> TOP 1
            result = a.TotalPathUsed.CompareTo(b.TotalPathUsed);
            if (result != 0) return result;

            // TG planning < 1st -> TOP > neu nuoc di =
            result = a.TotalPlanSeconds.CompareTo(b.TotalPlanSeconds);
            if (result != 0) return result;

            return a.UpdatedAt.CompareTo(b.UpdatedAt);
        }

        // Luu lai session ID moi cho moi nguoi choi
        public static void SaveOrUpdate(ScoreRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            List<ScoreRecord> records = LoadAll();

            int index = records.FindIndex(
                r => r.SessionId == record.SessionId);

            if (record.CompletedLevels == 0)
            {
                if (index >= 0)
                    records.RemoveAt(index);
            }
            else if (index >= 0)
            {
                records[index] = record;
            }
            else
            {
                records.Add(record);
            }

            WriteAll(records);
        }

        public static List<ScoreRecord> LoadTopRecords(int maxRecords)
        {
            List<ScoreRecord> records = LoadAll();
            records.Sort(Compare);

            if (maxRecords < 0)
                maxRecords = 0;

            if (records.Count > maxRecords)
            {
                records.RemoveRange(
                    maxRecords,
                    records.Count - maxRecords);
            }

            return records;
        }

        private static List<ScoreRecord> LoadAll()
        {
            List<ScoreRecord> records =
                new List<ScoreRecord>();

            if (!File.Exists(FilePath))
                return records;

            string[] lines =
                File.ReadAllLines(FilePath, Encoding.UTF8);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] p = line.Split('|');

                // Moi ban ghi phai co dung 12 cot.
                if (p.Length != 12)
                    continue;

                int l1Path, l1Time;
                int l2Path, l2Time;
                int l3Path, l3Time;

                if (!int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out l1Path) ||
                    !int.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out l1Time) ||
                    !int.TryParse(p[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out l2Path) ||
                    !int.TryParse(p[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out l2Time) ||
                    !int.TryParse(p[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out l3Path) ||
                    !int.TryParse(p[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out l3Time))
                {
                    continue;
                }

                DateTime updatedAt;
                DateTime.TryParse(
                    p[11],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out updatedAt);

                records.Add(new ScoreRecord
                {
                    SessionId = p[0],
                    Name = p[1],
                    Level1PathUsed = l1Path,
                    Level1PlanSeconds = l1Time,
                    Level2PathUsed = l2Path,
                    Level2PlanSeconds = l2Time,
                    Level3PathUsed = l3Path,
                    Level3PlanSeconds = l3Time,
                    UpdatedAt = updatedAt
                });
            }

            return records;
        }

        private static void WriteAll(List<ScoreRecord> records)
        {
            string folder = Path.GetDirectoryName(FilePath);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            // Chi co record tot nhat moi dc save
            records.Sort(Compare);
            if (records.Count > MaxStoredRecords)
                records.RemoveRange(MaxStoredRecords, records.Count - MaxStoredRecords);

            List<string> lines = new List<string>();
            lines.Add(Header);

            foreach (ScoreRecord record in records)
            {
                string safeName = (record.Name ?? "Player")
                    .Replace("|", "/")
                    .Replace("\r", " ")
                    .Replace("\n", " ")
                    .Trim();

                if (safeName.Length == 0)
                    safeName = "Player";

                lines.Add(string.Join("|", new string[]
                {
                    record.SessionId ?? "",
                    safeName,
                    record.Level1PathUsed.ToString(CultureInfo.InvariantCulture),
                    record.Level1PlanSeconds.ToString(CultureInfo.InvariantCulture),
                    record.Level2PathUsed.ToString(CultureInfo.InvariantCulture),
                    record.Level2PlanSeconds.ToString(CultureInfo.InvariantCulture),
                    record.Level3PathUsed.ToString(CultureInfo.InvariantCulture),
                    record.Level3PlanSeconds.ToString(CultureInfo.InvariantCulture),
                    record.TotalPathUsed.ToString(CultureInfo.InvariantCulture),
                    record.TotalPlanSeconds.ToString(CultureInfo.InvariantCulture),
                    record.CompletedLevels.ToString(CultureInfo.InvariantCulture),
                    record.UpdatedAt.ToString(
                        "yyyy-MM-dd HH:mm:ss",
                        CultureInfo.InvariantCulture)
                }));
            }

            // file temp ke cm no di (tranh mat' bxh)
            string temp = FilePath + ".tmp";
            File.WriteAllLines(temp, lines.ToArray(), Encoding.UTF8);
            File.Copy(temp, FilePath, true);
            File.Delete(temp);
        }
    }
}
