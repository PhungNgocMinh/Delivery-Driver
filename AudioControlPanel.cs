using System;
using System.Drawing;
using System.Windows.Forms;

namespace DeliveryDriverDemo
{
    /// <summary>
    /// Mute button plus volume bar. Used in the main menu and in the game HUD; every instance
    /// stays in sync with <see cref="AudioManager"/>.
    /// </summary>
    internal sealed class AudioControlPanel : Panel
    {
        private readonly Button _muteButton;
        private readonly TrackBar _volumeBar;
        private readonly Label _volumeLabel;
        private readonly bool _dark;
        private bool _syncing;

        public AudioControlPanel(bool dark)
        {
            _dark = dark;
            Color back = dark ? Color.FromArgb(28, 39, 50) : Color.FromArgb(235, 239, 242);
            Color fore = dark ? Color.FromArgb(239, 245, 248) : Color.FromArgb(28, 39, 50);
            BackColor = back;
            Size = new Size(290, 52);
            Margin = Padding.Empty;

            _muteButton = new Button
            {
                Width = 92,
                Height = 32,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                TabStop = false
            };
            _muteButton.FlatAppearance.BorderSize = 0;
            _muteButton.Click += delegate { AudioManager.Muted = !AudioManager.Muted; };

            _volumeBar = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                TickStyle = TickStyle.None,
                SmallChange = 5,
                LargeChange = 10,
                AutoSize = false,
                Width = 120,
                Height = 32,
                BackColor = back,
                TabStop = false
            };
            _volumeBar.ValueChanged += VolumeBarChanged;

            _volumeLabel = new Label
            {
                Width = 56,
                Height = 32,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = fore,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };

            Controls.Add(_muteButton);
            Controls.Add(_volumeBar);
            Controls.Add(_volumeLabel);

            AudioManager.SettingsChanged += AudioSettingsChanged;
            SyncFromAudio();
            PositionControls();
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            PositionControls();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) AudioManager.SettingsChanged -= AudioSettingsChanged;
            base.Dispose(disposing);
        }

        private void PositionControls()
        {
            if (_muteButton == null) return;
            int top = Math.Max(0, (Height - 32) / 2);
            _muteButton.SetBounds(0, top, 92, 32);
            _volumeBar.SetBounds(98, top, 120, 32);
            _volumeLabel.SetBounds(222, top, 62, 32);
        }

        private void VolumeBarChanged(object sender, EventArgs e)
        {
            if (_syncing) return;
            AudioManager.MasterVolume = _volumeBar.Value / 100f;
            if (AudioManager.Muted && _volumeBar.Value > 0)
                AudioManager.Muted = false;          // dragging the bar un-mutes
        }

        private void AudioSettingsChanged(object sender, EventArgs e)
        {
            SyncFromAudio();
        }

        private void SyncFromAudio()
        {
            _syncing = true;
            _volumeBar.Value = (int)Math.Round(AudioManager.MasterVolume * 100f);
            _syncing = false;

            bool muted = AudioManager.Muted;
            _muteButton.Text = muted ? "UNMUTE" : "MUTE";
            if (muted)
            {
                _muteButton.BackColor = Color.FromArgb(171, 56, 48);
                _muteButton.ForeColor = Color.White;
            }
            else
            {
                _muteButton.BackColor = _dark ? Color.FromArgb(62, 80, 96) : Color.FromArgb(218, 226, 231);
                _muteButton.ForeColor = _dark ? Color.White : Color.FromArgb(28, 39, 50);
            }
            _volumeLabel.Text = muted ? "MUTED" : "VOL " + _volumeBar.Value + "%";
        }
    }
}
