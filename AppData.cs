using System;
using System.Globalization;
using System.IO;

namespace DeliveryDriverDemo
{
    /// <summary>
    /// Folder for files the game keeps between sessions (leaderboard, settings, generated audio):
    /// %LocalAppData%\DeliveryDriverDemo. Unlike the build output folder it is not touched by
    /// Clean / Rebuild, so the leaderboard survives restarts and rebuilds.
    /// </summary>
    internal static class AppData
    {
        public static string Folder
        {
            get
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DeliveryDriverDemo");
                try { Directory.CreateDirectory(path); }
                catch (Exception) { /* callers cope with a missing folder */ }
                return path;
            }
        }
    }

    /// <summary>Small key=value settings file: master volume and mute state.</summary>
    internal static class AppSettings
    {
        private static bool _loaded;

        public static float Volume = 0.8f;
        public static bool Muted = false;

        private static string FilePath
        {
            get { return Path.Combine(AppData.Folder, "settings.txt"); }
        }

        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();

                    if (key == "volume")
                    {
                        float v;
                        if (Single.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                            Volume = Math.Max(0f, Math.Min(1f, v));
                    }
                    else if (key == "muted")
                    {
                        Muted = value == "true";
                    }
                }
            }
            catch (Exception)
            {
                // Unreadable settings fall back to the defaults above.
            }
        }

        public static void Save()
        {
            try
            {
                File.WriteAllLines(FilePath, new string[]
                {
                    "volume=" + Volume.ToString("0.00", CultureInfo.InvariantCulture),
                    "muted=" + (Muted ? "true" : "false")
                });
            }
            catch (Exception)
            {
                // Settings are a convenience; never crash because of them.
            }
        }
    }
}
