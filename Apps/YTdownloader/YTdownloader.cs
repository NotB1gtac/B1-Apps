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

		public YTdownloader()
		{
			InitializeComponent();
			SetupUI();
		}

		private void SetupUI()
		{
			// Initialize main layout
			mainLayout = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				RowCount = 4,
				ColumnCount = 1,
				Padding = new Padding(10)
			};
			Controls.Add(mainLayout);

			// URL TextBox
			urlTextBox = new TextBox
			{
				Font = new Font("Segoe UI", 12),
				Dock = DockStyle.Top,
				PlaceholderText = "Enter YouTube URL here..."
			};
			mainLayout.Controls.Add(urlTextBox);

			// Download Button
			downloadButton = new Button
			{
				Text = "Download",
				Font = new Font("Segoe UI", 12),
				Dock = DockStyle.Top
			};
			downloadButton.Click += DownloadButton_Click;
			mainLayout.Controls.Add(downloadButton);

			// Status Label
			statusLabel = new Label
			{
				Font = new Font("Segoe UI", 10),
				Dock = DockStyle.Top,
				TextAlign = ContentAlignment.MiddleCenter
			};
			mainLayout.Controls.Add(statusLabel);

			// Progress Bar
			progressBar = new ProgressBar
			{
				Style = ProgressBarStyle.Marquee,
				Dock = DockStyle.Top,
				Visible = false
			};
			mainLayout.Controls.Add(progressBar);

			// Download Speed Label
			downloadSpeedLabel = new Label
			{
				Font = new Font("Segoe UI", 10),
				Dock = DockStyle.Top,
				TextAlign = ContentAlignment.MiddleCenter,
				Visible = false
			};
			mainLayout.Controls.Add(downloadSpeedLabel);
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



		private void ExtractResource(string resourceName, string outputPath)
		{
			using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
			if (stream == null) throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
			using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
			stream.CopyTo(fs);
		}
	}
}
