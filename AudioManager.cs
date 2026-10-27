using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace DeliveryDriverDemo
{
    /// <summary>
    /// All game audio: looping background music (shared by the menu and the game), button clicks,
    /// the engine loop, crash / win / lose effects, plus master volume and mute.
    /// </summary>
    internal static class AudioManager
    {
        [DllImport("winmm.dll", CharSet = CharSet.Auto)]
        private static extern int mciSendString(string command, StringBuilder returnValue,
            int returnLength, IntPtr callbackWindow);

        private sealed class Channel
        {
            public readonly string Alias;
            public readonly string FilePath;
            public readonly double Gain;
            public readonly bool Loop;
            public bool IsOpen;
            public bool WantPlaying;

            public Channel(string alias, string filePath, double gain, bool loop)
            {
                Alias = alias;
                FilePath = filePath;
                Gain = gain;
                Loop = loop;
            }

            public void Open()
            {
                if (IsOpen || !File.Exists(FilePath)) return;
                int result = mciSendString("open \"" + FilePath + "\" type mpegvideo alias " + Alias,
                    null, 0, IntPtr.Zero);
                if (result != 0)
                    result = mciSendString("open \"" + FilePath + "\" alias " + Alias, null, 0, IntPtr.Zero);
                IsOpen = result == 0;
                if (IsOpen) ApplyVolume();
            }

            public void Play()
            {
                if (!IsOpen) Open();
                if (!IsOpen) return;
                ApplyVolume();
                int result = mciSendString("play " + Alias + " from 0" + (Loop ? " repeat" : String.Empty),
                    null, 0, IntPtr.Zero);
                if (result != 0 && Loop)
                    mciSendString("play " + Alias + " from 0", null, 0, IntPtr.Zero);
            }

            public void Stop()
            {
                if (IsOpen) mciSendString("stop " + Alias, null, 0, IntPtr.Zero);
            }

            public bool IsStopped()
            {
                if (!IsOpen) return false;
                StringBuilder mode = new StringBuilder(64);
                mciSendString("status " + Alias + " mode", mode, mode.Capacity, IntPtr.Zero);
                return mode.ToString() == "stopped";
            }

            public void ApplyVolume()
            {
                if (!IsOpen) return;
                int level = _muted ? 0 : (int)Math.Round(_volume * _volume * Gain * 1000.0);
                level = Math.Max(0, Math.Min(1000, level));
                mciSendString("setaudio " + Alias + " volume to " + level, null, 0, IntPtr.Zero);
            }

            public void Close()
            {
                if (!IsOpen) return;
                mciSendString("close " + Alias, null, 0, IntPtr.Zero);
                IsOpen = false;
            }
        }

        private static readonly List<Channel> Channels = new List<Channel>();
        private static Channel _music;
        private static Channel _engine;
        private static Channel _win;
        private static Channel _lose;
        private static Channel[] _clicks;
        private static Channel[] _crashes;
        private static int _clickIndex;
        private static int _crashIndex;

        private static Timer _timer;
        private static bool _initialized;
        private static bool _settingsDirty;
        private static float _volume = 0.8f;
        private static bool _muted;

        public static event EventHandler SettingsChanged;

        /// <summary>Master volume from 0 (silent) to 1 (full).</summary>
        public static float MasterVolume
        {
            get { return _volume; }
            set
            {
                value = Math.Max(0f, Math.Min(1f, value));
                if (Math.Abs(value - _volume) < 0.0001f) return;
                _volume = value;
                SettingsUpdated();
            }
        }

        public static bool Muted
        {
            get { return _muted; }
            set
            {
                if (value == _muted) return;
                _muted = value;
                SettingsUpdated();
            }
        }

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            AppSettings.Load();
            _volume = AppSettings.Volume;
            _muted = AppSettings.Muted;

            try
            {
                string folder = Path.Combine(AppData.Folder, "audio");
                SoundSynth.EnsureFiles(folder);

                _music = Add("ddmusic", Path.Combine(folder, SoundSynth.MusicFile), 0.45, true);
                _engine = Add("ddengine", Path.Combine(folder, SoundSynth.EngineFile), 0.50, true);
                _win = Add("ddwin", Path.Combine(folder, SoundSynth.WinFile), 0.80, false);
                _lose = Add("ddlose", Path.Combine(folder, SoundSynth.LoseFile), 0.80, false);

                // Several copies so quick repeated clicks / crashes can overlap.
                _clicks = new Channel[4];
                for (int i = 0; i < _clicks.Length; i++)
                    _clicks[i] = Add("ddclick" + i, Path.Combine(folder, SoundSynth.ClickFile), 0.80, false);
                _crashes = new Channel[2];
                for (int i = 0; i < _crashes.Length; i++)
                    _crashes[i] = Add("ddcrash" + i, Path.Combine(folder, SoundSynth.CrashFile), 1.00, false);

                foreach (Channel channel in Channels) channel.Open();

                // Restarts looping sounds that ended and saves changed volume / mute settings.
                _timer = new Timer { Interval = 250 };
                _timer.Tick += TimerTick;
                _timer.Start();
            }
            catch (Exception)
            {
                // Audio is optional. Without files or a sound device the game stays silent.
            }
        }

        private static Channel Add(string alias, string path, double gain, bool loop)
        {
            Channel channel = new Channel(alias, path, gain, loop);
            Channels.Add(channel);
            return channel;
        }

        public static void Shutdown()
        {
            if (!_initialized) return;
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
            foreach (Channel channel in Channels)
            {
                channel.Stop();
                channel.Close();
            }
            Channels.Clear();
            AppSettings.Volume = _volume;
            AppSettings.Muted = _muted;
            AppSettings.Save();
            _initialized = false;
        }

        // ------------------------------------------------------------------ playback

        /// <summary>Starts the background music. Safe to call repeatedly: it never restarts the track.</summary>
        public static void StartMusic()
        {
            if (_music == null || _music.WantPlaying) return;
            _music.WantPlaying = true;
            _music.Play();
        }

        public static void PlayClick()
        {
            if (_clicks == null) return;
            _clickIndex = (_clickIndex + 1) % _clicks.Length;
            _clicks[_clickIndex].Play();
        }

        public static void PlayCrash()
        {
            if (_crashes == null) return;
            _crashIndex = (_crashIndex + 1) % _crashes.Length;
            _crashes[_crashIndex].Play();
        }

        public static void PlayWin()
        {
            if (_win != null) _win.Play();
        }

        public static void PlayLose()
        {
            if (_lose != null) _lose.Play();
        }

        /// <summary>Engine loop that plays while the delivery car is driving.</summary>
        public static void SetEngineRunning(bool running)
        {
            if (_engine == null) return;
            if (running && !_engine.WantPlaying)
            {
                _engine.WantPlaying = true;
                _engine.Play();
            }
            else if (!running && _engine.WantPlaying)
            {
                _engine.WantPlaying = false;
                _engine.Stop();
            }
        }

        /// <summary>
        /// Gives every button below <paramref name="root"/> a click sound. The sound plays on mouse
        /// down (or Space / Enter) so it is heard even when the click opens a modal window.
        /// </summary>
        public static void AttachClickSounds(Control root)
        {
            Button button = root as Button;
            if (button != null)
            {
                button.MouseDown += ButtonMouseDown;
                button.KeyDown += ButtonKeyDown;
            }
            foreach (Control child in root.Controls)
                AttachClickSounds(child);
        }

        private static void ButtonMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) PlayClick();
        }

        private static void ButtonKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Return) PlayClick();
        }

        // ------------------------------------------------------------------ internals

        private static void SettingsUpdated()
        {
            foreach (Channel channel in Channels) channel.ApplyVolume();
            AppSettings.Volume = _volume;
            AppSettings.Muted = _muted;
            _settingsDirty = true;

            EventHandler handler = SettingsChanged;
            if (handler != null) handler(null, EventArgs.Empty);
        }

        private static void TimerTick(object sender, EventArgs e)
        {
            foreach (Channel channel in Channels)
            {
                if (channel.Loop && channel.WantPlaying && channel.IsStopped())
                    channel.Play();
            }

            if (_settingsDirty)
            {
                _settingsDirty = false;
                AppSettings.Save();
            }
        }
    }
}
