using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace B1_Apps.Apps.VideoPlayer
{
	public partial class VideoPlayerForm : Form
	{
		private LibVLC _libVLC;
		private Button _openBtn;
		private Button _subtitleBtn;
		private Button _addTrackBtn;
		private Button _playPauseBtn;
		private System.Windows.Forms.Timer _updateTimer;
		private bool _isPlaying = false;
		private bool _uiHidden = false;
		private Button _AudioBtn;
		private List<VideoTrack> _tracks = new List<VideoTrack>();
		private VideoTrack _activeTrack;

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
		}

		private void SetupUI()
		{
			this.BackColor = Color.Black;
			this.Text = "Multi-track Video Player";

			// Open button
			_openBtn = new Button
			{
				Text = "Open Video",
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.Black,
				ForeColor = Color.White,
				Location = new Point(10, 10)
			};
			_openBtn.Click += OpenVideo;
			this.Controls.Add(_openBtn);

			// Subtitle button
			_subtitleBtn = new Button
			{
				Text = "Subtitles",
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.Black,
				ForeColor = Color.White,
				Location = new Point(120, 10),
				Size = new Size(100, 25)
			};
			_subtitleBtn.Click += ShowSubtitleOptions;
			this.Controls.Add(_subtitleBtn);

			// Add Track button
			_addTrackBtn = new Button
			{
				Text = "Add Track",
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.Black,
				ForeColor = Color.White,
				Location = new Point(230, 10),
				Size = new Size(100, 25)
			};
			_addTrackBtn.Click += (s, e) => { CreateNewTrack(); UpdateLayout(); };
			this.Controls.Add(_addTrackBtn);
			_AudioBtn = new Button
			{
				Text = "Audio",
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.Black,
				ForeColor = Color.White,
				Location = new Point(340, 10),
				Size = new Size(100, 25)
			};
			_AudioBtn.Click += ShowAudioTrackOptions;
			this.Controls.Add(_AudioBtn);

			// Play/Pause button
			

			// Timer to update all players
			_updateTimer = new System.Windows.Forms.Timer { Interval = 200 };
			_updateTimer.Tick += UpdateTimeLabels;
			_updateTimer.Start();

			InitializeHideShortcut();

			this.Resize += (s, e) => UpdateLayout();
		}

		private void CreateNewTrack()
		{
			var player = new MediaPlayer(_libVLC);
			player.SetAudioOutput("directsound");
			var videoView = new VideoView
			{
				MediaPlayer = player,
				Dock = DockStyle.Fill,
				BackColor = Color.Black
			};

			var volumeBar = new TrackBar
			{
				Minimum = 0,
				Maximum = 100,
				Value = 50,
				TickStyle = TickStyle.None,
				Dock = DockStyle.Bottom,
				Height = 30
			};
			volumeBar.ValueChanged += (s, e) => player.Volume = volumeBar.Value;

			var timeLabel = new Label
			{
				ForeColor = Color.White,
				BackColor = Color.Black,
				Dock = DockStyle.Bottom,
				TextAlign = ContentAlignment.MiddleCenter,
				Height = 20
			};

			var timeline = new TrackBar
			{
				Minimum = 0,
				Maximum = 1000,
				TickStyle = TickStyle.None,
				Dock = DockStyle.Bottom,
				Height = 20
			};
			timeline.MouseUp += (s, e) =>
			{
				if (player.Media != null && player.Media.Duration > 0)
					player.Time = (long)(timeline.Value * (player.Media.Duration / 1000));
			};

			var removeBtn = new Button
			{
				Text = "✖",
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.Red,
				ForeColor = Color.White,
				Size = new Size(30, 30),
				Dock = DockStyle.Top
			};

			var playPauseBtn = new Button
			{
				Text = "▶", // initial icon
				FlatStyle = FlatStyle.Flat,
				BackColor = Color.Black,
				ForeColor = Color.White,
				Size = new Size(50, 25),
				Dock = DockStyle.Top,
				Visible = !_uiHidden // respect current UI hidden state
			};

			playPauseBtn.Click += (s, e) =>
			{
				// toggle play/pause for this specific player
				if (player.IsPlaying)
				{
					player.Pause();
					playPauseBtn.Text = "▶";
				}
				else
				{
					player.Play();
					playPauseBtn.Text = "⏸";
				}
			};

			var panel = new Panel
			{
				BackColor = Color.Gray,
				BorderStyle = BorderStyle.FixedSingle
			};

			var track = new VideoTrack
			{
				MediaPlayer = player,
				VideoView = videoView,
				VolumeBar = volumeBar,
				TimeLabel = timeLabel,
				Timeline = timeline,
				Panel = panel,
				RemoveBtn = removeBtn,
				PlayPauseBtn = playPauseBtn   // <<< assign the play/pause button here
			};

			removeBtn.Click += (s, e) =>
			{
				_tracks.Remove(track);
				player.Stop();
				player.Dispose();
				this.Controls.Remove(panel);
				UpdateLayout();
			};

			// Add controls in the order you want them stacked.
			// Controls added later appear on top when Dock = Top, so add removeBtn then playPauseBtn if you want playPause above remove, etc.
			panel.Controls.Add(videoView);
			panel.Controls.Add(timeline);
			panel.Controls.Add(volumeBar);
			panel.Controls.Add(timeLabel);
			panel.Controls.Add(removeBtn);
			panel.Controls.Add(playPauseBtn);

			// Click anywhere on panel or its children to select
			panel.Click += (s, e) => SetActiveTrack(track);
			foreach (Control ctl in panel.Controls)
			{
				// avoid overriding the playPause click behavior if desired:
				// if (ctl != playPauseBtn) ctl.Click += (s,e) => SetActiveTrack(track);
				ctl.Click += (s, e) => SetActiveTrack(track);
			}

			_tracks.Add(track);
			this.Controls.Add(panel);

			// ensure layout recalculated
			UpdateLayout();
		}


		private void SetActiveTrack(VideoTrack track)
		{
			_activeTrack = track;
			foreach (var t in _tracks)
			{
				t.Panel.BackColor = (t == track) ? Color.Yellow : Color.Gray;
			}
		}

		private void OpenVideo(object sender, EventArgs e)
		{
			if (_activeTrack == null) return;

			using (var dialog = new OpenFileDialog())
			{
				dialog.Filter = "Video Files|*.mp4;*.avi;*.mkv;*.mov;*.wmv|All Files|*.*";
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					var media = new Media(_libVLC, dialog.FileName);
					media.AddOption(":sub-autodetect-file");
					_activeTrack.MediaPlayer.Media = media;
					_activeTrack.MediaPlayer.Play();
				}
			}
		}

		private void ShowSubtitleOptions(object sender, EventArgs e)
		{
			try
			{


				if (_activeTrack == null || _activeTrack.MediaPlayer.Media == null) return;

				var menu = new ContextMenuStrip();
				var media = _activeTrack.MediaPlayer.Media;

				// Disable subtitles option
				menu.Items.Add("Disable Subtitles", null, (s, args) =>
				{
					_activeTrack.MediaPlayer.SetSpu(-1);
				});

				// Embedded subtitle tracks
				var subtitleTracks = GetSubtitleTracks(media);
				if (subtitleTracks.Count > 0)
				{
					var embeddedMenu = menu.Items.Add("Embedded Tracks") as ToolStripMenuItem;
					foreach (var track in subtitleTracks)
					{
						var displayName = !string.IsNullOrWhiteSpace(track.Language)
							? $"{track.TrackNumber}. {track.Language} ({track.Id})"
							: $"{track.TrackNumber}. Track {track.TrackNumber}";

						embeddedMenu.DropDownItems.Add(
							displayName,
							null,
							(s, args) => { _activeTrack.MediaPlayer.SetSpu(track.Id); });
					}
				}

				// Load external subtitle file
				menu.Items.Add("Load External Subtitle...", null, (s, args) => LoadExternalSubtitle());

				// Show menu at cursor
				menu.Show(Cursor.Position);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error showing subtitle options: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
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
					if (_activeTrack?.MediaPlayer != null)
					{
						// Attach the subtitle file immediately without reloading
						_activeTrack.MediaPlayer.AddSlave(
							MediaSlaveType.Subtitle,
							dialog.FileName,
							true
						);
					}
				}
			}
		}

		private class SubtitleTrackInfo
		{
			public int Id { get; set; }
			public string Language { get; set; }
			public int TrackNumber { get; set; }
		}
		private void ShowAudioTrackOptions(object sender, EventArgs e)
		{
			if (_activeTrack == null || _activeTrack.MediaPlayer.Media == null) return;

			var menu = new ContextMenuStrip();
			var media = _activeTrack.MediaPlayer.Media;

			menu.Items.Add("Mute Audio", null, (s, args) =>
			{
				_activeTrack.MediaPlayer.SetAudioTrack(-1);
			});

			var audioTracks = GetAudioTracks(media);
			if (audioTracks.Count > 0)
			{
				var embeddedMenu = menu.Items.Add("Available Audio Tracks") as ToolStripMenuItem;
				foreach (var track in audioTracks)
				{
					embeddedMenu.DropDownItems.Add(
						track.DisplayName,
						null,
						(s, args) => { _activeTrack.MediaPlayer.SetAudioTrack(track.Id); });
				}
			}

			menu.Items.Add("Load External Audio...", null, (s, args) => LoadExternalAudio());

			menu.Show(Cursor.Position);
		}

		private List<AudioTrackInfo> GetAudioTracks(Media media)
		{
			var tracks = new List<AudioTrackInfo>();
			int trackNumber = 1;

			if (media.Tracks != null)
			{
				foreach (var track in media.Tracks.OfType<MediaTrack>()
											  .Where(t => t.TrackType == TrackType.Audio))
				{
					tracks.Add(new AudioTrackInfo
					{
						Id = track.Id,
						
						Language = track.Language,
						TrackNumber = trackNumber++
					});
				}
			}
			return tracks;
		}

		private void LoadExternalAudio()
		{
			using (var dialog = new OpenFileDialog())
			{
				dialog.Filter = "Audio Files|*.mp3;*.aac;*.ogg;*.wav;*.flac|All Files|*.*";
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					var media = _activeTrack?.MediaPlayer?.Media;
					if (media != null)
					{
						// This option tells VLC to use external audio
						media.AddOption($":input-slave={dialog.FileName}");
						media.Parse();
					}
				}
			}
		}

		// Helper class for audio track info
		private class AudioTrackInfo
		{
			public int Id { get; set; }
			public string Codec { get; set; }
			public string Language { get; set; }
			public int TrackNumber { get; set; }
			public string DisplayName =>
				$"Track {TrackNumber} - {Language ?? "Unknown"} [{Codec ?? "Unknown Codec"}]";
		}



		private void TogglePlayPause(object sender, EventArgs e)
		{
			if (_activeTrack == null) return;

			if (_isPlaying)
			{
				_activeTrack.MediaPlayer.Pause();
				_playPauseBtn.Text = "▶";
			}
			else
			{
				_activeTrack.MediaPlayer.Play();
				_playPauseBtn.Text = "⏸";
			}
			_isPlaying = !_isPlaying;
		}

		private void UpdateTimeLabels(object sender, EventArgs e)
		{
			foreach (var track in _tracks)
			{
				if (track.MediaPlayer.Media == null) continue;
				track.TimeLabel.Text = $"{FormatTime(track.MediaPlayer.Time)} / {FormatTime(track.MediaPlayer.Media.Duration)}";
				if (track.MediaPlayer.Media.Duration > 0)
					track.Timeline.Value = (int)((double)track.MediaPlayer.Time / track.MediaPlayer.Media.Duration * 1000);
			}
		}

		private void UpdateLayout()
		{
			if (_tracks.Count == 0) return;

			int topOffset = _uiHidden ? 0 : 50;
			int cols = (int)Math.Ceiling(Math.Sqrt(_tracks.Count));
			int rows = (int)Math.Ceiling((double)_tracks.Count / cols);

			int cellWidth = this.ClientSize.Width / cols;
			int cellHeight = (this.ClientSize.Height - topOffset) / rows;

			for (int i = 0; i < _tracks.Count; i++)
			{
				int row = i / cols;
				int col = i % cols;

				var panel = _tracks[i].Panel;
				panel.Location = new Point(col * cellWidth, topOffset + row * cellHeight);
				panel.Size = new Size(cellWidth, cellHeight);
			}
		}

		private string FormatTime(long milliseconds)
		{
			var ts = TimeSpan.FromMilliseconds(milliseconds);
			return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
		}

		private void InitializeHideShortcut()
		{
			this.KeyPreview = true;
			this.KeyDown += (s, e) =>
			{
				if (e.KeyCode == Keys.H)
				{
					_uiHidden = !_uiHidden;

					_openBtn.Visible = !_uiHidden;
					_subtitleBtn.Visible = !_uiHidden;
					_addTrackBtn.Visible = !_uiHidden;
					_AudioBtn.Visible = !_uiHidden;

					foreach (var track in _tracks)
					{
						track.VolumeBar.Visible = !_uiHidden;
						track.TimeLabel.Visible = !_uiHidden;
						track.Timeline.Visible = !_uiHidden;

						if (track.RemoveBtn != null)
							track.RemoveBtn.Visible = !_uiHidden;

						if (track.PlayPauseBtn != null)
							track.PlayPauseBtn.Visible = !_uiHidden;
					}

					UpdateLayout();
				}
			};
		}


		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			_updateTimer?.Stop();
			foreach (var track in _tracks)
			{
				track.MediaPlayer?.Stop();
				track.MediaPlayer?.Dispose();
			}
			_libVLC?.Dispose();
			base.OnFormClosing(e);
		}
	}

	class VideoTrack
	{
		public MediaPlayer MediaPlayer { get; set; }
		public VideoView VideoView { get; set; }
		public TrackBar VolumeBar { get; set; }
		public Label TimeLabel { get; set; }
		public TrackBar Timeline { get; set; }
		public Panel Panel { get; set; }
		public Button RemoveBtn { get; set; }
		public Button PlayPauseBtn { get; set; }

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
