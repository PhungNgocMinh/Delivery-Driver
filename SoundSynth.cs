using System;
using System.IO;

namespace DeliveryDriverDemo
{
    /// Sound effect file wav chua trong %localappdata%
    internal static class SoundSynth
    {
        public const int SampleRate = 22050;
        private const double TwoPi = Math.PI * 2.0;

        public const string MusicFile = "music_v1.wav";
        public const string ClickFile = "click_v1.wav";
        public const string EngineFile = "engine_v1.wav";
        public const string CrashFile = "crash_v1.wav";
        public const string WinFile = "win_v1.wav";
        public const string LoseFile = "lose_v1.wav";

        private enum Wave { Sine, Triangle, Square, Saw }

        public static void EnsureFiles(string folder)
        {
            Directory.CreateDirectory(folder);
            WriteIfMissing(Path.Combine(folder, MusicFile), MakeMusic, 0.85);
            WriteIfMissing(Path.Combine(folder, ClickFile), MakeClick, 0.90);
            WriteIfMissing(Path.Combine(folder, EngineFile), MakeEngine, 0.80);
            WriteIfMissing(Path.Combine(folder, CrashFile), MakeCrash, 0.95);
            WriteIfMissing(Path.Combine(folder, WinFile), MakeWin, 0.85);
            WriteIfMissing(Path.Combine(folder, LoseFile), MakeLose, 0.85);
        }

        private static void WriteIfMissing(string path, Func<float[]> make, double peak)
        {
            if (File.Exists(path) && new FileInfo(path).Length > 44) return;
            SaveWav(path, make(), peak);
        }

        // music an cap tren mang

        private static float[] MakeMusic()
        {
            const double beat = 0.5;          
            const double bar = beat * 4.0;
            const int bars = 8;
            float[] buf = new float[(int)(bars * bar * SampleRate)];
            Random rng = new Random(2026);

            int[] progression = { 0, 1, 2, 3, 0, 1, 3, 1 };
            int[] bassRoot = { 36, 43, 45, 41 };
            int[][] triad =
            {
                new[] { 60, 64, 67 },
                new[] { 59, 62, 67 },
                new[] { 57, 60, 64 },
                new[] { 57, 60, 65 }
            };
            int[] bassPattern = { 0, 0, 7, 0 };
            int[] arpFirst = { 0, 1, 2, 3, 2, 1, 2, 1 };
            int[] arpSecond = { 2, 3, 2, 1, 3, 2, 1, 0 };
            int[][] melody =
            {
                new[] { 72, 76 }, new[] { 71, 74 }, new[] { 69, 72 }, new[] { 65, 69 },
                new[] { 76, 79 }, new[] { 74, 71 }, new[] { 72, 69 }, new[] { 71, 74 }
            };

            for (int b = 0; b < bars; b++)
            {
                int chord = progression[b];
                double t0 = b * bar;
                int[] tri = triad[chord];

                for (int i = 0; i < 3; i++)
                    AddNote(buf, t0, bar - 0.02, Midi(tri[i] - 12), 0.055, Wave.Sine, 0.18, 0.0);

                for (int k = 0; k < 4; k++)
                    AddNote(buf, t0 + k * beat, beat * 0.9, Midi(bassRoot[chord] + bassPattern[k]),
                        0.26, Wave.Triangle, 0.005, 2.5);

                int[] pattern = b < 4 ? arpFirst : arpSecond;
                int[] tones = { tri[0] + 12, tri[1] + 12, tri[2] + 12, tri[0] + 24 };
                for (int k = 0; k < 8; k++)
                    AddNote(buf, t0 + k * beat / 2.0, beat * 0.48, Midi(tones[pattern[k]]),
                        0.07, Wave.Square, 0.004, 7.0);

                for (int k = 0; k < 2; k++)
                    AddNote(buf, t0 + k * 2.0 * beat, 2.0 * beat * 0.95, Midi(melody[b][k]),
                        0.09, Wave.Triangle, 0.02, 1.4);

                for (int k = 0; k < 4; k++)
                {
                    double bt = t0 + k * beat;
                    if (k % 2 == 0) AddKick(buf, bt, 0.34);
                    else AddSnare(buf, bt, 0.11, rng);
                    AddHat(buf, bt + beat / 2.0, 0.035, rng);
                }
            }
            return buf;
        }

        // ------------------------------------------------------------------ effects

        private static float[] MakeClick()
        {
            float[] buf = new float[(int)(0.09 * SampleRate)];
            AddNote(buf, 0.0, 0.08, 1250.0, 0.55, Wave.Sine, 0.0008, 38.0);
            AddNote(buf, 0.0, 0.05, 620.0, 0.35, Wave.Triangle, 0.0008, 55.0);
            return buf;
        }

        private static float[] MakeEngine()
        {
            // Exactly one second built only from whole-cycle frequencies, so it loops seamlessly.
            float[] buf = new float[SampleRate];
            const double baseFreq = 55.0;
            Random rng = new Random(77);
            for (int i = 0; i < buf.Length; i++)
            {
                double t = i / (double)SampleRate;
                double sum = 0.0;
                for (int h = 1; h <= 7; h++)
                    sum += Math.Sin(TwoPi * baseFreq * h * t) / h;
                double wobble = 0.82 + 0.18 * Math.Sin(TwoPi * 11.0 * t);
                double noise = (rng.NextDouble() * 2.0 - 1.0) * 0.06;
                buf[i] = (float)(sum * wobble * 0.55 + noise);
            }
            return buf;
        }

        private static float[] MakeCrash()
        {
            float[] buf = new float[(int)(0.9 * SampleRate)];
            Random rng = new Random(404);
            double phase = 0.0;
            for (int i = 0; i < buf.Length; i++)
            {
                double t = i / (double)SampleRate;
                double noise = (rng.NextDouble() * 2.0 - 1.0) * Math.Exp(-t * 6.5) * 0.75;
                double freq = 45.0 + 150.0 * Math.Exp(-t * 9.0);
                phase += freq / SampleRate;
                double thud = Math.Sin(TwoPi * phase) * Math.Exp(-t * 5.0) * 0.9;
                double clang = (Math.Sin(TwoPi * 440.0 * t) + 0.7 * Math.Sin(TwoPi * 660.0 * t * 1.01)
                                + 0.5 * Math.Sin(TwoPi * 1175.0 * t)) * Math.Exp(-t * 14.0) * 0.18;
                double v = noise + thud + clang;
                buf[i] = (float)Math.Tanh(v * 1.3);
            }
            return buf;
        }

        private static float[] MakeWin()
        {
            float[] buf = new float[(int)(1.4 * SampleRate)];
            int[] notes = { 72, 76, 79, 84 };
            for (int i = 0; i < notes.Length; i++)
                AddNote(buf, i * 0.13, 0.35, Midi(notes[i]), 0.30, Wave.Triangle, 0.004, 3.0);
            AddNote(buf, 0.52, 0.85, Midi(72), 0.16, Wave.Sine, 0.01, 2.0);
            AddNote(buf, 0.52, 0.85, Midi(76), 0.16, Wave.Sine, 0.01, 2.0);
            AddNote(buf, 0.52, 0.85, Midi(79), 0.16, Wave.Sine, 0.01, 2.0);
            AddNote(buf, 0.52, 0.85, Midi(84), 0.20, Wave.Triangle, 0.01, 2.0);
            return buf;
        }

        private static float[] MakeLose()
        {
            float[] buf = new float[(int)(1.3 * SampleRate)];
            int[] notes = { 67, 64, 60 };
            for (int i = 0; i < notes.Length; i++)
            {
                double dur = i == notes.Length - 1 ? 0.7 : 0.26;
                AddNote(buf, i * 0.26, dur, Midi(notes[i] - 12), 0.26, Wave.Saw, 0.005, 3.0);
            }
            return buf;
        }

        // ------------------------------------------------------------------ building blocks

        private static double Midi(int note)
        {
            return 440.0 * Math.Pow(2.0, (note - 69) / 12.0);
        }

        private static double Osc(Wave wave, double phase)
        {
            switch (wave)
            {
                case Wave.Sine: return Math.Sin(TwoPi * phase);
                case Wave.Triangle: return 4.0 * Math.Abs(phase - 0.5) - 1.0;
                case Wave.Square: return phase < 0.5 ? 0.8 : -0.8;
                default: return 2.0 * phase - 1.0;
            }
        }

        private static void AddNote(float[] buf, double start, double duration, double freq, double amp,
            Wave wave, double attack, double decay)
        {
            int first = (int)(start * SampleRate);
            int count = (int)(duration * SampleRate);
            for (int i = 0; i < count; i++)
            {
                int index = first + i;
                if (index < 0 || index >= buf.Length) continue;

                double t = i / (double)SampleRate;
                double env = (attack > 0.0 && t < attack) ? t / attack : 1.0;
                env *= Math.Exp(-decay * t);
                double left = duration - t;
                if (left < 0.012) env *= left / 0.012;    

                double phase = (freq * t) % 1.0;
                buf[index] += (float)(amp * env * Osc(wave, phase));
            }
        }

        private static void AddKick(float[] buf, double start, double amp)
        {
            int first = (int)(start * SampleRate);
            int count = (int)(0.2 * SampleRate);
            double phase = 0.0;
            for (int i = 0; i < count; i++)
            {
                int index = first + i;
                if (index >= buf.Length) break;
                double t = i / (double)SampleRate;
                phase += (45.0 + 95.0 * Math.Exp(-t * 32.0)) / SampleRate;
                buf[index] += (float)(Math.Sin(TwoPi * phase) * Math.Exp(-t * 17.0) * amp);
            }
        }

        private static void AddSnare(float[] buf, double start, double amp, Random rng)
        {
            int first = (int)(start * SampleRate);
            int count = (int)(0.13 * SampleRate);
            for (int i = 0; i < count; i++)
            {
                int index = first + i;
                if (index >= buf.Length) break;
                double t = i / (double)SampleRate;
                double noise = (rng.NextDouble() * 2.0 - 1.0) * Math.Exp(-t * 26.0);
                double tone = Math.Sin(TwoPi * 190.0 * t) * Math.Exp(-t * 30.0) * 0.6;
                buf[index] += (float)((noise + tone) * amp);
            }
        }

        private static void AddHat(float[] buf, double start, double amp, Random rng)
        {
            int first = (int)(start * SampleRate);
            int count = (int)(0.045 * SampleRate);
            double previous = 0.0;
            for (int i = 0; i < count; i++)
            {
                int index = first + i;
                if (index >= buf.Length) break;
                double t = i / (double)SampleRate;
                double raw = rng.NextDouble() * 2.0 - 1.0;
                double high = raw - previous;               // crude high-pass
                previous = raw;
                buf[index] += (float)(high * Math.Exp(-t * 85.0) * amp);
            }
        }

        private static void SaveWav(string path, float[] data, double peak)
        {
            double max = 0.0;
            for (int i = 0; i < data.Length; i++)
                max = Math.Max(max, Math.Abs(data[i]));
            double scale = max > 0.0 ? peak / max : 1.0;

            string temp = path + ".tmp";
            using (FileStream stream = new FileStream(temp, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                int dataBytes = data.Length * 2;
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataBytes);
                writer.Write(new[] { 'W', 'A', 'V', 'E' });
                writer.Write(new[] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);              // PCM
                writer.Write((short)1);              // mono
                writer.Write(SampleRate);
                writer.Write(SampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataBytes);
                for (int i = 0; i < data.Length; i++)
                {
                    double v = Math.Max(-1.0, Math.Min(1.0, data[i] * scale));
                    writer.Write((short)Math.Round(v * 32767.0));
                }
            }

            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }
    }
}
