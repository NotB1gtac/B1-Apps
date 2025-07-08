using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Wave;

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

		private void InitializePlayback(string file)
		{
			// Clean up any previous playback
			outputDevice?.Stop();
			outputDevice?.Dispose();
			audioFile?.Dispose();

			// Load the selected audio
			audioFile = new AudioFileReader(file) { Volume = tbVolume.Value / 100f };

			// Determine output device
			int deviceNum = comboOutput.SelectedValue is int d ? d : -1;
			outputDevice = (deviceNum >= 0 && deviceNum < WaveOut.DeviceCount)
				? new WaveOutEvent { DeviceNumber = deviceNum }
				: new WaveOutEvent();

			// Initialize and start playback
			outputDevice.Init(audioFile);
			btnPlay.Text = "Pause";
			UpdateNowPlaying();

			// Subscribe once to PlaybackStopped
			outputDevice.PlaybackStopped -= OnPlaybackStopped;
			outputDevice.PlaybackStopped += OnPlaybackStopped;

			outputDevice.Play();
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

		

		

		private void UpdateNowPlaying()
		{
			if (currentIndex >= 0 && currentIndex < playlist.Count)
			{
				currentSongLabel.Text = $"Now playing: {Path.GetFileName(playlist[currentIndex])}";
				songListBox.SelectedIndex = currentIndex;
			}
			else currentSongLabel.Text = "Now playing: —";
		}

		private void OnPlaybackStopped(object sender, StoppedEventArgs e)
		{
			// Avoid re-entrancy: check actual end of file
			if (audioFile != null && audioFile.Position < audioFile.Length)
			{
				// Probably paused or stopped manually – do nothing
				return;
			}

			if (currentIndex + 1 < playlist.Count)
			{
				currentIndex++;
				songListBox.Invoke((Action)(() =>
				{
					songListBox.SelectedIndex = currentIndex;
				}));
				InitializePlayback(playlist[currentIndex]);
			}
			else
			{
				btnPlay.Invoke((Action)(() => btnPlay.Text = "Play"));
			}
		}


		private void TogglePlayPause()
		{
			if (outputDevice == null) return;
			if (outputDevice.PlaybackState == PlaybackState.Playing)
			{
				outputDevice.Pause();
				btnPlay.Text = "Play";
			}
			else
			{
				outputDevice.Play();
				btnPlay.Text = "Pause";
			}
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
			outputDevice?.Stop();
			audioFile?.Dispose();
			outputDevice?.Dispose();
			base.OnFormClosing(e);
		}

		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(600, 400);
			this.Text = "AudioPlayer";
		}

		
	}
}
