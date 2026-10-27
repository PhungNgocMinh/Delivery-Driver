using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;

namespace DeliveryDriverDemo
{
    internal static class ScoreBoard
    {
        public static readonly string FilePath =
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "thanhtichs.txt");

        private const string Header =
            "SessionId|Name|L1Score|L1Time|" +
            "L2Score|L2Time|L3Score|L3Time|" +
            "TotalScore|TotalTime|CompletedLevels|UpdatedAt";

        public static void SaveOrUpdate(ScoreRecord record) //kiem tra de xem co ton tai ban ghi hay chua neu co thi update neu chua thi them vao
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

            records.Sort(delegate (ScoreRecord a, ScoreRecord b)
            {
                int compareScore =
                    b.TotalScore.CompareTo(a.TotalScore);

                if (compareScore != 0)
                    return compareScore;

                return a.TotalTimeSeconds.CompareTo(
                    b.TotalTimeSeconds);
            });

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

                // Mỗi bản ghi phải có đúng 12 cột.
                if (p.Length != 12)
                    continue;

                int l1Score, l1Time;
                int l2Score, l2Time;
                int l3Score, l3Time;

                if (!int.TryParse(p[2], out l1Score) ||
                    !int.TryParse(p[3], out l1Time) ||
                    !int.TryParse(p[4], out l2Score) ||
                    !int.TryParse(p[5], out l2Time) ||
                    !int.TryParse(p[6], out l3Score) ||
                    !int.TryParse(p[7], out l3Time))
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
                    Level1Score = l1Score,
                    Level1TimeSeconds = l1Time,
                    Level2Score = l2Score,
                    Level2TimeSeconds = l2Time,
                    Level3Score = l3Score,
                    Level3TimeSeconds = l3Time,
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
                    record.Level1Score.ToString(CultureInfo.InvariantCulture),
                    record.Level1TimeSeconds.ToString(CultureInfo.InvariantCulture),
                    record.Level2Score.ToString(CultureInfo.InvariantCulture),
                    record.Level2TimeSeconds.ToString(CultureInfo.InvariantCulture),
                    record.Level3Score.ToString(CultureInfo.InvariantCulture),
                    record.Level3TimeSeconds.ToString(CultureInfo.InvariantCulture),
                    record.TotalScore.ToString(CultureInfo.InvariantCulture),
                    record.TotalTimeSeconds.ToString(CultureInfo.InvariantCulture),
                    record.CompletedLevels.ToString(CultureInfo.InvariantCulture),
                    record.UpdatedAt.ToString(
                        "yyyy-MM-dd HH:mm:ss",
                        CultureInfo.InvariantCulture)
                }));
            }

            File.WriteAllLines(
                FilePath,
                lines.ToArray(),
                Encoding.UTF8);
        }
    }
}
