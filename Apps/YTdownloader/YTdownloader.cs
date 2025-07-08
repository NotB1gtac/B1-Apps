using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace B1_Apps.Apps.YTdownloader
{
	public partial class YTdownloader : Form
	{
		private TextBox urlTextBox;
		private Button downloadButton;
		private Label statusLabel;
		private TableLayoutPanel mainLayout;
		private ProgressBar progressBar;
		private Label downloadSpeedLabel;
		private Button downloadPlaylistButton;
		private DataGridView playlistDataGridView;
		private DataGridViewCheckBoxColumn downloadColumn;
		private DataGridViewTextBoxColumn titleColumn;
		private DataGridViewComboBoxColumn formatColumn;
		private Button startPlaylistButton;



		public YTdownloader()
		{
			InitializeComponent();
			SetupUI();
		}

		private void SetupUI()
		{
			mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1, Padding = new Padding(10) };
			Controls.Add(mainLayout);

			urlTextBox = new TextBox { Font = new Font("Segoe UI", 12), Dock = DockStyle.Top, PlaceholderText = "Enter YouTube URL here..." };
			mainLayout.Controls.Add(urlTextBox);

			downloadButton = new Button { Text = "Download", Font = new Font("Segoe UI", 12), Dock = DockStyle.Top };
			downloadButton.Click += DownloadButton_Click;
			mainLayout.Controls.Add(downloadButton);

			statusLabel = new Label { Font = new Font("Segoe UI", 10), Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter };
			mainLayout.Controls.Add(statusLabel);

			progressBar = new ProgressBar { Style = ProgressBarStyle.Marquee, Dock = DockStyle.Top, Visible = false };
			mainLayout.Controls.Add(progressBar);

			downloadSpeedLabel = new Label { Font = new Font("Segoe UI", 10), Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter, Visible = false };
			mainLayout.Controls.Add(downloadSpeedLabel);

			downloadPlaylistButton = new Button { Text = "Download Playlist", Font = new Font("Segoe UI", 10), Dock = DockStyle.Top };
			downloadPlaylistButton.Click += DownloadPlaylistButton_Click;
			mainLayout.Controls.Add(downloadPlaylistButton);

			startPlaylistButton = new Button { Text = "Start Playlist Download", Font = new Font("Segoe UI", 10), Dock = DockStyle.Top, Visible = false };
			startPlaylistButton.Click += StartDownloadButton_Click;
			mainLayout.Controls.Add(startPlaylistButton);

			playlistDataGridView = new DataGridView
			{
				Dock = DockStyle.Fill,
				Visible = false,
				AutoGenerateColumns = false,
				AllowUserToAddRows = false,
				RowHeadersVisible = false
			};

			var downloadCol = new DataGridViewCheckBoxColumn { Name = "downloadColumn", HeaderText = "Download", Width = 70, TrueValue = true, FalseValue = false };
			var titleCol = new DataGridViewTextBoxColumn { Name = "titleColumn", HeaderText = "Title", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill };
			var formatCol = new DataGridViewComboBoxColumn { Name = "formatColumn", HeaderText = "Format", Width = 120, DataSource = new string[] { "Audio", "Video+Audio" } };
			var urlColumn = new DataGridViewTextBoxColumn { Name = "urlColumn", HeaderText = "Video URL", Visible = false };

			playlistDataGridView.Columns.AddRange(downloadCol, titleCol, formatCol, urlColumn);

			mainLayout.Controls.Add(playlistDataGridView);
		}


		private async void DownloadButton_Click(object sender, EventArgs e)
		{
			string url = urlTextBox.Text.Trim();
			if (string.IsNullOrEmpty(url))
			{
				statusLabel.Text = "Please enter a valid YouTube URL.";
				return;
			}

			// 1) Zeptat se na formát: Yes = mp3 (audio), No = mp4 (video + audio), Cancel = zrušit
			var formatChoice = MessageBox.Show(
				"Choose format:\nYes for MP3 (audio only)\nNo for MP4 (video + audio)",
				"Select Format",
				MessageBoxButtons.YesNoCancel);

			if (formatChoice == DialogResult.Cancel)
			{
				statusLabel.Text = "Download cancelled.";
				return;
			}

			// 2) Nastavit argumenty podle volby
			string formatArgument = formatChoice == DialogResult.Yes
				? "bestaudio[ext=m4a]"        // mp3 (audio only)
				: "bestvideo+bestaudio/best"; // mp4 (video + audio)

			// 3) Vybrat složku pro uložení
			using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
			{
				folderDialog.Description = "Select Folder to Save Download";
				folderDialog.ShowNewFolderButton = true;

				if (folderDialog.ShowDialog() != DialogResult.OK)
				{
					statusLabel.Text = "Download cancelled.";
					return;
				}

				string downloadFolder = folderDialog.SelectedPath;
				string ytDlpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "yt-dlp.exe");

				if (!File.Exists(ytDlpPath))
				{
					statusLabel.Text = "yt-dlp.exe not found.";
					return;
				}

				// Sestavit příkaz
				string arguments = $"-f {formatArgument} --progress --no-warnings --quiet -o \"{downloadFolder}\\%(title)s.%(ext)s\" \"{url}\"";

				try
				{
					statusLabel.Text = "Initializing download...";
					progressBar.Style = ProgressBarStyle.Marquee;
					progressBar.Visible = true;
					downloadSpeedLabel.Visible = true;

					var process = new System.Diagnostics.Process
					{
						StartInfo = new System.Diagnostics.ProcessStartInfo
						{
							FileName = ytDlpPath,
							Arguments = arguments,
							RedirectStandardOutput = true,
							RedirectStandardError = true,
							UseShellExecute = false,
							CreateNoWindow = true
						}
					};

					process.OutputDataReceived += (s, ev) =>
					{
						if (ev.Data != null)
						{
							var match = System.Text.RegularExpressions.Regex.Match(ev.Data, @"(\d+)%.*?(\d+(\.\d+)?)\s*([KMGT]B/s)");
							if (match.Success)
							{
								int percent = int.Parse(match.Groups[1].Value);
								string speed = match.Groups[4].Value;

								progressBar.Invoke(new Action(() =>
								{
									progressBar.Style = ProgressBarStyle.Blocks;
									progressBar.Value = Math.Min(percent, 100);
									downloadSpeedLabel.Text = $"Speed: {speed}";
								}));
							}
						}
					};

					process.Start();
					process.BeginOutputReadLine();
					await process.WaitForExitAsync();

					progressBar.Style = ProgressBarStyle.Blocks;
					progressBar.Value = 100;
					statusLabel.Text = "Download completed successfully.";
				}
				catch (Exception ex)
				{
					statusLabel.Text = $"Exception: {ex.Message}";
				}
				finally
				{
					progressBar.Visible = false;
					downloadSpeedLabel.Visible = false;
				}
			}
		}
		private async void DownloadPlaylistButton_Click(object sender, EventArgs e)
		{
			var url = urlTextBox.Text.Trim();
			if (string.IsNullOrEmpty(url)) { statusLabel.Text = "Invalid URL."; return; }

			statusLabel.Text = "Loading playlist...";
			var videos = await FetchPlaylistDetails(url);
			playlistDataGridView.Rows.Clear();
			foreach (var v in videos)
				playlistDataGridView.Rows.Add(true, v.Title, "Audio", v.Url);
			

			playlistDataGridView.Visible = true;
			startPlaylistButton.Visible = true;
			statusLabel.Text = $"Loaded {videos.Count} videos.";
		}




		private async Task<List<VideoDetail>> FetchPlaylistDetails(string playlistUrl)
		{
			var videos = new List<VideoDetail>();
			string ytDlpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "yt-dlp.exe");
			if (!File.Exists(ytDlpPath))
				throw new FileNotFoundException("yt-dlp.exe not found.", ytDlpPath);

			var psi = new System.Diagnostics.ProcessStartInfo
			{
				FileName = ytDlpPath,
				Arguments = $"--flat-playlist --dump-single-json \"{playlistUrl}\"",
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using var process = System.Diagnostics.Process.Start(psi);
			using var reader = process.StandardOutput;
			string json = await reader.ReadToEndAsync();
			await process.WaitForExitAsync();

			// Parse JSON for "entries"
			using var doc = System.Text.Json.JsonDocument.Parse(json);
			if (doc.RootElement.TryGetProperty("entries", out var entries))
			{
				foreach (var item in entries.EnumerateArray())
				{
					string id = item.GetProperty("id").GetString() ?? "";
					string title = item.GetProperty("title").GetString() ?? id;
					videos.Add(new VideoDetail
					{
						Title = title,
						Url = $"https://www.youtube.com/watch?v={id}"
					});
				}
			}
			return videos;
		}

		public class VideoDetail
		{
			public string Title { get; set; }
			public string Url { get; set; }
		}


		private void InitializeComponent()
		{
			playlistDataGridView = new DataGridView();
			((System.ComponentModel.ISupportInitialize)(playlistDataGridView)).BeginInit();
			SuspendLayout();

			playlistDataGridView.Location = new Point(12, 70);
			playlistDataGridView.Name = "playlistDataGridView";
			playlistDataGridView.Size = new Size(760, 400);
			playlistDataGridView.TabIndex = 1;
			playlistDataGridView.AllowUserToAddRows = false;
			playlistDataGridView.RowHeadersVisible = false;
			playlistDataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			playlistDataGridView.Visible = false; // start hidden

			var downloadColumn = new DataGridViewCheckBoxColumn
			{
				Name = "downloadColumn",
				HeaderText = "Download",
				Width = 70,
				TrueValue = true,
				FalseValue = false
			};

			var titleColumn = new DataGridViewTextBoxColumn
			{
				Name = "titleColumn",
				HeaderText = "Title",
				ReadOnly = true,
				AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
			};

			var formatColumn = new DataGridViewComboBoxColumn
			{
				Name = "formatColumn",
				HeaderText = "Format",
				Width = 120,
				DataSource = new string[] { "Audio", "Video+Audio" }
			};

			var urlColumn = new DataGridViewTextBoxColumn
			{
				Name = "urlColumn",
				HeaderText = "Video URL",
				Visible = false
			};

			// Add columns in correct order
			playlistDataGridView.Columns.AddRange(
				downloadColumn,
				titleColumn,
				formatColumn,
				urlColumn
			);

			Controls.Add(playlistDataGridView);

			((System.ComponentModel.ISupportInitialize)(playlistDataGridView)).EndInit();
			ResumeLayout(false);
		}

		private void ExtractResource(string resourceName, string outputPath)
		{
			using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
			if (stream == null) throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
			using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
			stream.CopyTo(fs);
		}
		private async void StartDownloadButton_Click(object sender, EventArgs e)
		{
			using var dlg = new FolderBrowserDialog
			{
				Description = "Select download folder",
				ShowNewFolderButton = true
			};
			if (dlg.ShowDialog() != DialogResult.OK)
			{
				statusLabel.Text = "Cancelled.";
				return;
			}
			string folder = dlg.SelectedPath;
			string yt = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "yt-dlp.exe");
			if (!File.Exists(yt))
			{
				statusLabel.Text = "yt-dlp.exe not found.";
				return;
			}

			foreach (DataGridViewRow row in playlistDataGridView.Rows)
			{
				if (!(row.Cells["downloadColumn"].Value is bool dl) || !dl)
					continue;
				string videoUrl = row.Cells["urlColumn"].Value.ToString();

				
					
				string title = row.Cells["titleColumn"].Value?.ToString();
				string chosenFormat = row.Cells["formatColumn"].Value?.ToString();
				string fmt = chosenFormat == "Audio" ? "bestaudio[ext=m4a]" : "bestvideo+bestaudio";
				string safeTitle = string.Concat(title.Split(Path.GetInvalidFileNameChars()));
				string args = $"-f {fmt} --progress --no-warnings --quiet -o \"{folder}\\{safeTitle}.%(ext)s\" \"{videoUrl}\"";

				statusLabel.Text = $"Downloading '{title}'...";
				var proc = new System.Diagnostics.Process
				{
					StartInfo = new System.Diagnostics.ProcessStartInfo
					{
						FileName = yt,
						Arguments = args,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						UseShellExecute = false,
						CreateNoWindow = true
					}
				};

				proc.Start();
				await proc.WaitForExitAsync();
			}

			statusLabel.Text = "Playlist download finished.";
		}


	}
}
