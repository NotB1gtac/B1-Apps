using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Dsp;
using System.Diagnostics;

namespace B1_Apps.Apps.AudioPlayer
{
	public partial class AudioPlayer : Form
	{
		private IWavePlayer outputDevice;
		private AudioFileReader audioFile;
		private List<string> playlist = new List<string>();
		private int currentIndex = -1;
		private TrackBar tbVolume;
		private Label volumeLabel;

		private ListBox songListBox;
		private ComboBox comboOutput;
		private Label currentSongLabel;
		private Button btnPlay;
		private TrackBar trackPosition;
		private Label labelElapsed;
		private Label labelTotal;
		private System.Windows.Forms.Timer playbackTimer;
		private bool isSeeking = false;
		private Random rng = new Random();
		private bool repeatPlaylist = false;
		private bool repeatTrack = false;
		private Button btnShuffle;
		private Button btnRepeatPlaylist;
		private Button btnRepeatTrack;
		// Add to your existing variables
		private BufferedWaveProvider waveProvider;
		private int fftLength = 1024; // Power of two for FFT
		private float[] fftBuffer;
		private Complex[] fftResult;
		private int sampleRate;
		private int channels;
		private PictureBox visualizerBox;
		private System.Windows.Forms.Timer visualizerTimer; // Add with your other class variables
		public AudioPlayer()
		{
			InitializeComponent();
			SetupUI();
		}

		private void SetupUI()
		{
			Text = "Audio Player";
			Size = new Size(600, 400);
			Font = new Font("Segoe UI", 10);

			var mainLayout = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				RowCount = 5,
				ColumnCount = 1,
				Padding = new Padding(10)
			};
			Controls.Add(mainLayout);

			// Browse folder tlačítko
			var btnBrowse = new Button { Text = "Browse Folder...", Dock = DockStyle.Top };
			btnBrowse.Click += BtnBrowse_Click;
			mainLayout.Controls.Add(btnBrowse);

			// Výběr audio zařízení
			comboOutput = new ComboBox { Dock = DockStyle.Top };
			var devices = EnumerateOutputDevices();
			comboOutput.DataSource = devices;
			comboOutput.DisplayMember = "Name";
			comboOutput.ValueMember = "DeviceNumber";
			mainLayout.Controls.Add(comboOutput);

			// Seznam skladeb
			songListBox = new ListBox { Dock = DockStyle.Fill };
			songListBox.SelectedIndexChanged += SongListBox_SelectedIndexChanged;
			mainLayout.Controls.Add(songListBox);

			// Label aktuální skladby
			currentSongLabel = new Label { Text = "Now playing: —", Dock = DockStyle.Top, AutoSize = true };
			mainLayout.Controls.Add(currentSongLabel);

			// Panel ovládání přehrávače
			var panelControls = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				Height = 50,
				FlowDirection = FlowDirection.LeftToRight
			};
			var btnPrev = new Button { Text = "⏮ Prev", Width = 80 };
			btnPlay = new Button { Text = "Play", Width = 80 };
			var btnNext = new Button { Text = "Next ⏭", Width = 80 };
			tbVolume = new TrackBar
			{
				Minimum = 0,
				Maximum = 100,
				Value = 50,
				Width = 150,
				TickStyle = TickStyle.None
			};
			// Label pro procentuální hodnotu hlasitosti
			volumeLabel = new Label { Text = $"{tbVolume.Value} %", AutoSize = true, Padding = new Padding(5, 8, 5, 0) };

			// Event handler pro volume změnu
			tbVolume.Scroll += (s, e) =>
			{
				if (audioFile != null)
					audioFile.Volume = tbVolume.Value / 100f;
				volumeLabel.Text = $"{tbVolume.Value} %";
			};

			btnPrev.Click += (s, e) => PlayPrevious();
			btnPlay.Click += (s, e) => TogglePlayPause();
			btnNext.Click += (s, e) => PlayNext();

			panelControls.Controls.AddRange(new Control[]
			{
				btnPrev,
				btnPlay,
				btnNext,
				tbVolume,
				volumeLabel  // přidáno
			});

			mainLayout.Controls.Add(panelControls);
			// In SetupUI(), after your volume controls:
			trackPosition = new TrackBar
			{
				Dock = DockStyle.Top,
				Minimum = 0,
				TickStyle = TickStyle.None
			};
			trackPosition.MouseDown += (s, e) => isSeeking = true;
			trackPosition.MouseUp += (s, e) =>
			{
				if (audioFile != null)
					audioFile.CurrentTime = TimeSpan.FromSeconds(trackPosition.Value);
				isSeeking = false;
			};
			mainLayout.Controls.Add(trackPosition);

			// Labels for time display
			var timePanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 20 };
			labelElapsed = new Label { Text = "00:00", AutoSize = true };
			labelTotal = new Label { Text = "00:00", AutoSize = true };
			timePanel.Controls.AddRange(new Control[] { labelElapsed, new Label { Text = "/" }, labelTotal });
			mainLayout.Controls.Add(timePanel);
			btnShuffle = new Button { Text = "🔀 Shuffle", Width = 100 };
			btnShuffle.Click += BtnShuffle_Click;
			panelControls.Controls.Add(btnShuffle);
			btnRepeatPlaylist = new Button
			{
				Text = "🔁 Repeat List",
				Width = 120,
				BackColor = Color.LightGray  // Default inactive color
			};
			btnRepeatPlaylist.Click += BtnRepeatPlaylist_Click;

			btnRepeatTrack = new Button
			{
				Text = "🔂 Repeat Track",
				Width = 120,
				BackColor = Color.LightGray  // Default inactive color
			};
			btnRepeatTrack.Click += BtnRepeatTrack_Click;

			panelControls.Controls.Add(btnRepeatPlaylist);
			panelControls.Controls.Add(btnRepeatTrack);
			visualizerBox = new PictureBox
			{
				Dock = DockStyle.Bottom,
				Height = 150,
				BackColor = Color.Black
			};
			mainLayout.Controls.Add(visualizerBox);

			// Add a timer for visualization updates
			var visualizerTimer = new System.Windows.Forms.Timer { Interval = 50 };
			visualizerTimer.Tick += VisualizerTimer_Tick;
			visualizerTimer.Start();
			// Timer to update UI
			playbackTimer = new System.Windows.Forms.Timer { Interval = 500 };
			playbackTimer.Tick += PlaybackTimer_Tick;

		}

		private void PlaybackTimer_Tick(object sender, EventArgs e)
		{
			if (audioFile == null || isSeeking) return;

			var current = audioFile.CurrentTime;
			var total = audioFile.TotalTime;
			trackPosition.Maximum = (int)total.TotalSeconds;
			trackPosition.Value = Math.Min((int)current.TotalSeconds, trackPosition.Maximum);
			labelElapsed.Text = current.ToString(@"mm\:ss");
			labelTotal.Text = total.ToString(@"mm\:ss");
		}

		private List<(string Name, int DeviceNumber)> EnumerateOutputDevices()
		{
			var list = new List<(string, int)>
			{
				("Default", -1)  // -1 znamená výchozí zařízení
			};
			for (int i = 0; i < WaveOut.DeviceCount; i++)
			{
				var cap = WaveOut.GetCapabilities(i);
				list.Add((cap.ProductName, i));
			}
			return list;
		}

		private void SongListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			int idx = songListBox.SelectedIndex;
			if (idx >= 0 && idx < playlist.Count)
			{
				currentIndex = idx;
				InitializePlayback(playlist[currentIndex]);
			}
		}

		private bool isManualStop = false; // Add this class-level variable

		private void InitializePlayback(string file)
		{
			// Clean up any previous playback
			outputDevice?.Stop();
			outputDevice?.Dispose();
			audioFile?.Dispose();

			// Load the selected audio
			audioFile = new AudioFileReader(file) { Volume = tbVolume.Value / 100f };

			// Initialize FFT buffers
			fftLength = 1024;
			fftBuffer = new float[fftLength];
			fftResult = new Complex[fftLength];
			previousBarHeights = new float[50];
			

			// Create and configure the sample aggregator
			var sampleAggregator = new SampleAggregator(audioFile, fftLength);
			sampleAggregator.FftCalculated += (s, a) =>
			{
				// Copy FFT results to our buffer
				Buffer.BlockCopy(a.Result, 0, fftBuffer, 0, fftLength * sizeof(float));

				// Convert to Complex numbers for FFT
				for (int i = 0; i < fftLength; i++)
				{
					fftResult[i].X = fftBuffer[i];
					fftResult[i].Y = 0; // Imaginary part
				}

				// Perform FFT (in-place)
				FastFourierTransform.FFT(true, (int)Math.Log(fftLength, 2), fftResult);
			};

			// Initialize output device
			int deviceNum = comboOutput.SelectedValue is int d ? d : -1;
			outputDevice = (deviceNum >= 0 && deviceNum < WaveOut.DeviceCount)
				? new WaveOutEvent { DeviceNumber = deviceNum }
				: new WaveOutEvent();

			// Initialize with our processing chain
			outputDevice.Init(sampleAggregator);

			// Start playback
			playbackTimer.Start();
			btnPlay.Text = "Pause";
			UpdateNowPlaying();

			
			outputDevice.PlaybackStopped += OnPlaybackStopped;
			outputDevice.Play();
			isManualStop = false;
		}
		private float[] previousBarHeights = new float[50];
		

		private void VisualizerTimer_Tick(object sender, EventArgs e)
		{
			if (visualizerBox == null || visualizerBox.Width <= 0 || fftResult == null)
				return;

			try
			{
				using (var bitmap = new Bitmap(visualizerBox.Width, visualizerBox.Height))
				using (var g = Graphics.FromImage(bitmap))
				{
					g.Clear(Color.Black);

					int bandCount = 50;
					int barWidth = visualizerBox.Width / bandCount;

					for (int i = 0; i < bandCount; i++)
					{
						// Get frequency band (logarithmic scale)
						int startBin = (int)(Math.Pow(2, i / 3.0) - 1);
						int endBin = (int)(Math.Pow(2, (i + 1) / 3.0) - 1);
						endBin = Math.Min(endBin, fftLength / 2 - 1);

						// Calculate average magnitude for this band
						double sum = 0;
						for (int bin = startBin; bin <= endBin; bin++)
						{
							double magnitude = Math.Sqrt(
								fftResult[bin].X * fftResult[bin].X +
								fftResult[bin].Y * fftResult[bin].Y);
							sum += magnitude;
						}
						double averageMagnitude = sum / (endBin - startBin + 1);

						// Convert to dB and scale
						double dB = 20 * Math.Log10(averageMagnitude);
						double scaledHeight = (dB + 60) / 60; // Scale -60dB to 0dB to 0-1 range
						scaledHeight = Math.Max(0, Math.Min(1, scaledHeight));

						// Apply smoothing
						float height = (previousBarHeights[i] * 0.7f) + ((float)scaledHeight * 0.38f);
						previousBarHeights[i] = height;

						// Update peaks
						

						// Draw bar
						int barHeight = (int)(height * visualizerBox.Height);
						Color color = Color.FromArgb(
							255,
							(int)(i * 255 / bandCount),
							150,
							255 - (int)(i * 255 / bandCount));

						using (var brush = new SolidBrush(color))
						{
							g.FillRectangle(brush,
								i * barWidth,
								visualizerBox.Height - barHeight,
								barWidth - 1,
								barHeight);
						}

						// Draw peak
						
					}

					// Update display
					var oldImage = visualizerBox.Image;
					visualizerBox.Image = (Bitmap)bitmap.Clone();
					oldImage?.Dispose();
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Visualizer error: {ex.Message}");
			}
		}
		
		
		private void OnPlaybackStopped(object sender, StoppedEventArgs e)
		{
			// Handle exceptions
			if (e.Exception != null)
			{
				MessageBox.Show($"Playback error: {e.Exception.Message}", "Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Don't proceed if this was a manual stop
			if (isManualStop)
			{
				playbackTimer?.Stop();
				return;
			}

			// Don't proceed if we're disposing or there's no playlist
			if (audioFile == null || outputDevice == null || playlist == null)
				return;

			// Continue to next track only if playback naturally completed
			if (audioFile.Position >= audioFile.Length - 1000)
			{
				playbackTimer?.Stop();

				if (repeatTrack)
				{
					// Repeat the current track
					this.Invoke((Action)(() =>
					{
						InitializePlayback(playlist[currentIndex]);
					}));
				}
				else if (repeatPlaylist && currentIndex + 1 >= playlist.Count)
				{
					// Repeat the playlist from start
					currentIndex = 0;
					this.Invoke((Action)(() =>
					{
						songListBox.SelectedIndex = currentIndex;
						InitializePlayback(playlist[currentIndex]);
					}));
				}
				else if (currentIndex + 1 < playlist.Count)
				{
					// Go to next track
					currentIndex++;
					this.Invoke((Action)(() =>
					{
						songListBox.SelectedIndex = currentIndex;
						InitializePlayback(playlist[currentIndex]);
					}));
				}
				else
				{
					// End of playlist
					this.Invoke((Action)(() => btnPlay.Text = "Play"));
				}
			}
		}

		private void TogglePlayPause()
		{
			if (outputDevice == null) return;

			if (outputDevice.PlaybackState == PlaybackState.Playing)
			{
				outputDevice.Pause();
				btnPlay.Text = "Play";
				isManualStop = true;
			}
			else
			{
				if (outputDevice.PlaybackState == PlaybackState.Stopped)
				{
					// If completely stopped, reinitialize playback
					InitializePlayback(playlist[currentIndex]);
				}
				else
				{
					outputDevice.Play();
					btnPlay.Text = "Pause";
				}
				isManualStop = false;
			}
		}
		private void BtnRepeatPlaylist_Click(object sender, EventArgs e)
		{
			repeatPlaylist = !repeatPlaylist;
			repeatTrack = false;  // Only one repeat mode can be active

			// Update button appearance
			btnRepeatPlaylist.BackColor = repeatPlaylist ? Color.LightGreen : Color.LightGray;
			btnRepeatTrack.BackColor = Color.LightGray;
		}

		private void BtnRepeatTrack_Click(object sender, EventArgs e)
		{
			repeatTrack = !repeatTrack;
			repeatPlaylist = false;  // Only one repeat mode can be active

			// Update button appearance
			btnRepeatTrack.BackColor = repeatTrack ? Color.LightGreen : Color.LightGray;
			btnRepeatPlaylist.BackColor = Color.LightGray;
		}

		private void BtnBrowse_Click(object sender, EventArgs e)
		{
			using var dlg = new FolderBrowserDialog();
			if (dlg.ShowDialog() != DialogResult.OK) return;

			var supported = new[] { ".mp3", ".m4a", ".wav" };
			playlist = Directory
				.EnumerateFiles(dlg.SelectedPath)
				.Where(f => supported.Contains(Path.GetExtension(f).ToLowerInvariant()))
				.ToList();

			// Optional: Shuffle immediately after loading
			// playlist = playlist.OrderBy(x => rng.Next()).ToList();

			songListBox.Items.Clear();
			songListBox.Items.AddRange(playlist.Select(Path.GetFileName).ToArray());

			if (playlist.Count > 0)
			{
				currentIndex = 0;
				InitializePlayback(playlist[currentIndex]);
			}
			else
			{
				MessageBox.Show("No supported audio files found in that folder.");
			}
		}
		private void BtnShuffle_Click(object sender, EventArgs e)
		{
			if (playlist == null || playlist.Count == 0) return;

			// Store the currently playing file before shuffling
			string currentFile = currentIndex >= 0 ? playlist[currentIndex] : null;

			// Shuffle the playlist
			playlist = playlist.OrderBy(x => rng.Next()).ToList();
			songListBox.Items.Clear();
			songListBox.Items.AddRange(playlist.Select(Path.GetFileName).ToArray());

			// Find and select the current file in the new shuffled list
			if (currentFile != null)
			{
				currentIndex = playlist.IndexOf(currentFile);
				if (currentIndex >= 0)
				{
					songListBox.SelectedIndex = currentIndex;
				}
				else
				{
					currentIndex = 0;
					if (playlist.Count > 0)
					{
						songListBox.SelectedIndex = 0;
					}
				}
			}
			else if (playlist.Count > 0)
			{
				currentIndex = 0;
				songListBox.SelectedIndex = 0;
			}
		}




		private void UpdateNowPlaying()
		{
			if (currentIndex >= 0 && currentIndex < playlist.Count)
			{
				currentSongLabel.Text = $"Now playing: {Path.GetFileName(playlist[currentIndex])}";
				songListBox.SelectedIndex = currentIndex;
			}
			else currentSongLabel.Text = "Now playing: —";
		}

		




		

		private void PlayNext()
		{
			if (currentIndex + 1 >= playlist.Count) return;
			currentIndex++;
			InitializePlayback(playlist[currentIndex]);
		}

		private void PlayPrevious()
		{
			if (currentIndex <= 0) return;
			currentIndex--;
			InitializePlayback(playlist[currentIndex]);
		}

		
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			// Stop the playbackTimer if used
			playbackTimer?.Stop();

			if (outputDevice != null)
			{
				outputDevice.PlaybackStopped -= OnPlaybackStopped;
				outputDevice.Stop();
				outputDevice.Dispose();
				visualizerBox.Image?.Dispose();
				audioFile?.Dispose();
				audioFile = null;
				outputDevice = null;
			}

			

			base.OnFormClosing(e);
		}

		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 800);
			this.Text = "AudioPlayer";

			// Initialize and configure the timer
			visualizerTimer = new System.Windows.Forms.Timer(this.components)
			{
				Interval = 16 // ~60 FPS
			};
			visualizerTimer.Tick += VisualizerTimer_Tick;

			// Enable double buffering for smoother rendering
			this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
						 ControlStyles.AllPaintingInWmPaint |
						 ControlStyles.UserPaint, true);
			this.DoubleBuffered = true;
		}
		protected override void OnResize(EventArgs e)
		{
			if (visualizerTimer != null) // Safety check
			{
				if (this.WindowState == FormWindowState.Minimized)
				{
					visualizerTimer.Interval = 100; // 10 FPS when minimized
				}
				else
				{
					visualizerTimer.Interval = 16; // 60 FPS when visible

					// Bonus: Reduce FPS if window is small to save CPU
					if (this.ClientSize.Width < 400 || this.ClientSize.Height < 300)
					{
						visualizerTimer.Interval = 33; // ~30 FPS for small windows
					}
				}
			}
			base.OnResize(e);
		}


	}
}
