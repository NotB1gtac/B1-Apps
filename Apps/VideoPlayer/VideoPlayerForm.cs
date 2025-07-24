using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using LibVLCSharp;
using System;
using System.IO;
using System.Windows.Forms;
using Windows.Media.Core;
using System.Linq;

namespace B1_Apps.Apps.VideoPlayer
{
	public partial class VideoPlayerForm : Form
	{
		private LibVLC _libVLC;
		private MediaPlayer _mediaPlayer;
		private VideoView _videoView;
		private Label _timeLabel;
		private TrackBar _volumeBar;
		private Button _openBtn;
		private System.Windows.Forms.Timer _updateTimer;
		private Button _playPauseBtn;
		private bool _isPlaying = false;
		private TrackBar _timeline;
		private bool _isDraggingTimeline = false;
		private bool _uiHidden = false;
		private Button _subtitleBtn; // Button to manage subtitles

		public VideoPlayerForm()
		{
			InitializeVLC();
			SetupUI();
		}

		private void InitializeVLC()
		{
			string libVlcPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libvlc", "win-x64");
			Core.Initialize(libVlcPath);
			_libVLC = new LibVLC();
			_mediaPlayer = new MediaPlayer(_libVLC);
		}

		private void SetupUI()
		{
			// Video display (fills the form)
			_videoView = new VideoView { Dock = DockStyle.Fill };
			_videoView.MediaPlayer = _mediaPlayer;
			this.Controls.Add(_videoView);

			// Time label (bottom-left)
			_timeLabel = new Label
			{
				AutoSize = true,
				ForeColor = Color.White,
				BackColor = Color.Black,
				Padding = new Padding(5),
				Location = new Point(10, this.ClientSize.Height - 30)
			};
			this.Controls.Add(_timeLabel);
			_timeLabel.BringToFront();

			// Volume slider (bottom-right)
			_volumeBar = new TrackBar
			{
				Width = 100,
				Minimum = 0,
				Maximum = 100,
				Value = 50,
				TickStyle = TickStyle.None,
				Location = new Point(this.ClientSize.Width - 110, this.ClientSize.Height - 30)
			};
			_volumeBar.ValueChanged += (s, e) => _mediaPlayer.Volume = _volumeBar.Value;
			this.Controls.Add(_volumeBar);
			_volumeBar.BringToFront();

			// Open button (top-right)
			_openBtn = new Button
			{
				Text = "Open Video",
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.Black,
				ForeColor = Color.White,
				Location = new Point(this.ClientSize.Width - 110, 10)
			};
			_openBtn.Click += OpenVideo;
			this.Controls.Add(_openBtn);
			_openBtn.BringToFront();
			// Subtitle button (add near your Open button)
			_subtitleBtn = new Button
			{
				Text = "Subtitle Track",
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.Black,
				ForeColor = Color.White,
				Location = new Point(this.ClientSize.Width - 220, 10), // Left of Open button
				Size = new Size(100, 25)
			};
			_subtitleBtn.Click += ShowSubtitleOptions;
			this.Controls.Add(_subtitleBtn);
			_subtitleBtn.BringToFront();

			// Add timeline and play/pause button first
			AddTimeline();
			AddPlayPauseButton();

			// Timer to update time label
			_updateTimer = new System.Windows.Forms.Timer { Interval = 200 };
			_updateTimer.Tick += UpdateTimeLabel;
			_updateTimer.Start();

			// Ensure controls resize with the window
			this.Resize += (s, e) => UpdateControlPositions();

			InitializeHideShortcut();
		}

		private void UpdateControlPositions()
		{
			// Update all control positions
			_timeLabel.Location = new Point(10, this.ClientSize.Height - 30);
			_volumeBar.Location = new Point(this.ClientSize.Width - 110, this.ClientSize.Height - 30);
			_openBtn.Location = new Point(this.ClientSize.Width - 110, 10);

			// Special positioning for play/pause button
			if (_playPauseBtn != null && _timeline != null)
			{
				_playPauseBtn.Location = new Point(
					this.ClientSize.Width / 2 - _playPauseBtn.Width / 2,
					this.ClientSize.Height - _timeline.Height - _playPauseBtn.Height - 10
				);
			}
		}

		private void InitializeHideShortcut()
		{
			this.KeyPreview = true;
			this.KeyDown += (s, e) =>
			{
				if (e.KeyCode == Keys.H)
				{
					ToggleUI();
				}
			};
		}

		private void ToggleUI()
		{
			_uiHidden = !_uiHidden;

			// Always show controls when not in fullscreen
			bool shouldShow = !_uiHidden || this.WindowState != FormWindowState.Maximized;

			_timeLabel.Visible = shouldShow;
			_volumeBar.Visible = shouldShow;
			_openBtn.Visible = shouldShow;
			_playPauseBtn.Visible = shouldShow;
			_timeline.Visible = shouldShow;
			_subtitleBtn.Visible = shouldShow;
			

		}

		private void AddTimeline()
		{
			_timeline = new TrackBar
			{
				Dock = DockStyle.Bottom,
				Height = 30,  // Slightly taller for better usability
				TickStyle = TickStyle.None,
				Maximum = 1000
			};
			_timeline.MouseDown += (s, e) => _isDraggingTimeline = true;
			_timeline.MouseUp += (s, e) =>
			{
				_isDraggingTimeline = false;
				if (_mediaPlayer.Media != null)
				{
					_mediaPlayer.Time = (long)(_timeline.Value * (_mediaPlayer.Media.Duration / 1000));
				}
			};
			this.Controls.Add(_timeline);
			_timeline.BringToFront();
		}

		private void AddPlayPauseButton()
		{
			_playPauseBtn = new Button
			{
				Text = "⏸",
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.Black,
				ForeColor = Color.White,
				Size = new Size(50, 50),  // Larger for better visibility
				Font = new Font("Segoe UI", 12)
			};

			// Position will be set in UpdateControlPositions()
			_playPauseBtn.Click += TogglePlayPause;
			this.Controls.Add(_playPauseBtn);
			_playPauseBtn.BringToFront();
			UpdateControlPositions();  // Set initial position
		}
		private void OpenVideo(object sender, EventArgs e)
		{
			using (var dialog = new OpenFileDialog())
			{
				dialog.Filter = "Video Files|*.mp4;*.avi;*.mkv;*.mov;*.wmv|All Files|*.*";
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					var media = new Media(_libVLC, dialog.FileName);

					// Enable subtitle parsing
					media.AddOption(":sub-autodetect-file");

					_mediaPlayer.Media = media;
					_mediaPlayer.Play();

					media.Parse();
					media.ParsedChanged += (s, args) =>
					{
						this.Invoke((MethodInvoker)delegate
						{
							_timeline.Maximum = (int)(media.Duration / 1000);

							// Get all subtitle tracks with names
							var subtitleTracks = GetSubtitleTracks(media);

							// Auto-enable first subtitle track if available
							if (subtitleTracks.Count > 0)
							{
								_mediaPlayer.SetSpu(subtitleTracks[0].Id);
								UpdateSubtitleStatus($"Sub: {subtitleTracks[0].DisplayName}");
							}
						});
					};
				}
			}
		}
		private void UpdateSubtitleStatus(string status)
		{
			//TO DO: Implement a label or status bar to show subtitle status
		}

		private void ShowSubtitleOptions(object sender, EventArgs e)
		{
			if (_mediaPlayer.Media == null) return;

			var menu = new ContextMenuStrip();
			var media = _mediaPlayer.Media;

			// Option 1: Disable subtitles good for native speakears 
			menu.Items.Add("Disable Subtitles", null, (s, args) =>
			{
				_mediaPlayer.SetSpu(-1);
				UpdateSubtitleStatus("Sub: Off");
			});

			// Option 2: Embedded subtitles
			var subtitleTracks = GetSubtitleTracks(media);
			if (subtitleTracks.Count > 0)
			{
				var embeddedMenu = menu.Items.Add("Embedded Tracks") as ToolStripMenuItem;
				foreach (var track in subtitleTracks)
				{
					embeddedMenu.DropDownItems.Add(
						track.DisplayName,
						null,
						(s, args) => {
							_mediaPlayer.SetSpu(track.Id);
							UpdateSubtitleStatus($"Sub: {track.DisplayName}");
						});
				}
			}

			// Option 3: Load external subtitle file should work
			menu.Items.Add("Load External Subtitle...", null, (s, args) => LoadExternalSubtitle());

			menu.Show(Cursor.Position);
		}
		private List<SubtitleTrackInfo> GetSubtitleTracks(Media media)
		{
			var tracks = new List<SubtitleTrackInfo>();
			int trackNumber = 1;

			if (media.Tracks != null)
			{
				foreach (var track in media.Tracks.OfType<MediaTrack>()
											  .Where(t => t.TrackType == TrackType.Text))
				{
					tracks.Add(new SubtitleTrackInfo
					{
						Id = track.Id,
						Name = "Track",
						Language = track.Language,
						TrackNumber = trackNumber++
					});
				}
			}
			return tracks;
		}


		private void LoadExternalSubtitle()
		{
			using (var dialog = new OpenFileDialog())
			{
				dialog.Filter = "Subtitle Files|*.srt;*.ass;*.ssa;*.sub|All Files|*.*";
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					var media = _mediaPlayer.Media;
					if (media != null)
					{
						media.AddOption($":sub-file={dialog.FileName}");
						media.Parse();
						UpdateSubtitleStatus($"Sub: {Path.GetFileName(dialog.FileName)}");
					}
				}
			}
		}

		private void TogglePlayPause(object sender, EventArgs e)
		{
			if (_isPlaying)
			{
				_mediaPlayer.Pause();
				_playPauseBtn.Text = "▶"; // Play icon
			}
			else
			{
				_mediaPlayer.Play();
				_playPauseBtn.Text = "⏸"; // Pause icon
			}
			_isPlaying = !_isPlaying;
		}

		private void UpdateTimeLabel(object sender, EventArgs e)
		{
			if (_mediaPlayer.Media == null) return;
			_timeLabel.Text = $"{FormatTime(_mediaPlayer.Time)} / {FormatTime(_mediaPlayer.Media.Duration)}";
		}

		private string FormatTime(long milliseconds)
		{
			var ts = TimeSpan.FromMilliseconds(milliseconds);
			return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			_updateTimer?.Stop();
			_mediaPlayer?.Stop();
			_mediaPlayer?.Dispose();
			_libVLC?.Dispose();
			base.OnFormClosing(e);
		}
	}
	 class SubtitleTrackInfo
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Language { get; set; }
		public int TrackNumber { get; set; }

		public string DisplayName =>
			!string.IsNullOrWhiteSpace(Name) ? $"{Name} ({Language})" :
			!string.IsNullOrWhiteSpace(Language) ? $"Track {TrackNumber} ({Language})" :
			$"Track {TrackNumber}";
	}
}