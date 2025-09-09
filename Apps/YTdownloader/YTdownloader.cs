using System;
using System.Diagnostics;
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
			var errorBuffer = new System.Text.StringBuilder();

			// Ask format
			var formatChoice = MessageBox.Show(
				"Choose format:\nYes = MP3 (audio only)\nNo = MP4 (video + audio)",
				"Select Format",
				MessageBoxButtons.YesNoCancel);

			if (formatChoice == DialogResult.Cancel)
			{
				statusLabel.Text = "Download cancelled.";
				return;
			}

			bool audioOnly = formatChoice == DialogResult.Yes;

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

				// ✅ Build yt-dlp args
				string arguments = audioOnly
					? $"-f bestaudio --extract-audio --audio-format mp3 --audio-quality 0 " +
					  $"--add-metadata --embed-thumbnail --no-playlist " +
					  $"-o \"{downloadFolder}\\%(title)s.%(ext)s\" \"{url}\""
					: $"-f bestvideo+bestaudio " +
					  $"--add-metadata --embed-thumbnail --no-playlist " +
					  $"-o \"{downloadFolder}\\%(title)s.%(ext)s\" \"{url}\"";

				try
				{
					statusLabel.Text = "Initializing download...";
					progressBar.Style = ProgressBarStyle.Marquee;
					progressBar.Visible = true;
					downloadSpeedLabel.Visible = true;

					var process = new Process
					{
						StartInfo = new ProcessStartInfo
						{
							FileName = ytDlpPath,
							Arguments = arguments,
							RedirectStandardOutput = true,
							RedirectStandardError = true,
							UseShellExecute = false,
							CreateNoWindow = true,
							StandardOutputEncoding = System.Text.Encoding.UTF8,
							StandardErrorEncoding = System.Text.Encoding.UTF8
						},
						EnableRaisingEvents = true
					};

					process.OutputDataReceived += (s, ev) =>
					{
						if (!string.IsNullOrWhiteSpace(ev.Data))
						{
							Console.WriteLine("OUT: " + ev.Data);
						}
					};

					process.ErrorDataReceived += (s, ev) =>
					{
						if (!string.IsNullOrWhiteSpace(ev.Data))
						{
							errorBuffer.AppendLine(ev.Data);

							// ✅ parse progress
							var match = Regex.Match(ev.Data, @"(\d+(?:\.\d+)?)%.*?([0-9.]+\s*[KMGT]?B/s)");
							if (match.Success)
							{
								int percent = (int)double.Parse(match.Groups[1].Value);
								string speed = match.Groups[2].Value;

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
					process.BeginErrorReadLine();
					await process.WaitForExitAsync();

					if (process.ExitCode == 0)
					{
						progressBar.Value = 100;
						statusLabel.Text = "Download completed successfully.";
					}
					else
					{
						string errors = errorBuffer.ToString();
						if (string.IsNullOrWhiteSpace(errors))
							errors = "Unknown error (no details captured).";

						MessageBox.Show(errors, "yt-dlp Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						statusLabel.Text = "Download failed.";
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
				statusLabel.BackColor = Color.LightYellow;
				return;
			}

			string folder = dlg.SelectedPath;
			string yt = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "yt-dlp.exe");
			if (!File.Exists(yt))
			{
				MessageBox.Show("yt-dlp.exe not found in Resources folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			statusLabel.Text = "Starting playlist download...";
			statusLabel.BackColor = Color.LightYellow;

			var tasks = new List<Task>();
			foreach (DataGridViewRow row in playlistDataGridView.Rows)
			{
				if (!(row.Cells["downloadColumn"].Value is bool dl) || !dl)
					continue;

				tasks.Add(DownloadVideoAsync(row, folder, yt));
			}

			await Task.WhenAll(tasks);

			statusLabel.Text = "Playlist download finished.";
			statusLabel.BackColor = Color.LightGreen;
		}

		private async Task DownloadVideoAsync(DataGridViewRow row, string folder, string ytDlpPath)
		{
			string videoUrl = row.Cells["urlColumn"].Value?.ToString() ?? "";
			string title = row.Cells["titleColumn"].Value?.ToString() ?? "video";
			string chosenFormat = row.Cells["formatColumn"].Value?.ToString();
			string safeTitle = string.Concat(title.Split(Path.GetInvalidFileNameChars()));

			string args;
			if (chosenFormat == "Audio")
			{
				args = $"-f bestaudio " +
					   $"--extract-audio --audio-format mp3 --audio-quality 0 " +
					   $"--add-metadata --embed-thumbnail --no-warnings " +
					   $"-o \"{Path.Combine(folder, safeTitle + ".%(ext)s")}\" " +
					   $"\"{videoUrl}\"";
			}
			else
			{
				args = $"-f bestvideo[ext=mp4]+bestaudio[ext=m4a]/mp4 " +
					   $"--merge-output-format mp4 --add-metadata --embed-thumbnail --no-warnings " +
					   $"-o \"{Path.Combine(folder, safeTitle + ".%(ext)s")}\" " +
					   $"\"{videoUrl}\"";
			}

			await Task.Run(() =>
			{
				var proc = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = ytDlpPath,
						Arguments = args,
						UseShellExecute = false,
						RedirectStandardError = true,
						CreateNoWindow = true
					}
				};

				proc.Start();
				string stderr = proc.StandardError.ReadToEnd();
				proc.WaitForExit();

				if (proc.ExitCode != 0)
				{
					MessageBox.Show(
						$"Download failed for: {title}\n\nSTDERR:\n{stderr}",
						"Download Error",
						MessageBoxButtons.OK,
						MessageBoxIcon.Error
					);
				}
			});

			// Update statusLabel safely from background thread
			if (statusLabel.InvokeRequired)
				statusLabel.Invoke(new Action(() => statusLabel.Text = $"Downloaded: {title}"));
			else
				statusLabel.Text = $"Downloaded: {title}";
			statusLabel.BackColor = Color.LightGreen;
		}



	}
}
