using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;

namespace B1_Apps.Apps.FormatConverter
{
	public partial class FormatConverterForm : Form
	{
		// Supported output formats
		private readonly Dictionary<string, Action<FFMpegArgumentOptions>> _outputFormats = new Dictionary<string, Action<FFMpegArgumentOptions>>()
		{
			{ "MP3", opts => opts.WithAudioCodec("libmp3lame").WithAudioBitrate(192) },
			{ "WAV", opts => opts.WithAudioCodec("pcm_s16le") },
			{ "MP4 (H.264 + AAC)", opts => opts.WithVideoCodec("libx264").WithConstantRateFactor(23).WithAudioCodec("aac") },
			{ "AVI", opts => opts.WithVideoCodec("mpeg4").WithAudioCodec("libmp3lame") }  // Changed from LibXvid to mpeg4
        };

		private TextBox inputText;
		private Button browseInput, convertBtn;
		private ComboBox formatBox;
		private ProgressBar progressBar;
		private Label statusLabel;

		public FormatConverterForm()
		{
			InitializeComponent();
			SetupFFmpeg();
			SetupUI();
		}

		private void SetupFFmpeg()
		{
			var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg");
			if (Directory.Exists(ffmpegPath))
			{
				GlobalFFOptions.Configure(options => options.BinaryFolder = ffmpegPath);
			}
		}

		private void InitializeComponent()
		{
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new System.Drawing.Size(500, 250);
			Text = "Media Format Converter";
		}

		private void SetupUI()
		{
			// Input controls
			var inputPanel = new Panel { Dock = DockStyle.Top, Height = 60 };
			var inputLabel = new Label { Text = "Input File:", Dock = DockStyle.Left, Width = 80 };
			inputText = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
			browseInput = new Button { Text = "Browse...", Dock = DockStyle.Right, Width = 80 };
			browseInput.Click += BrowseInput_Click;

			inputPanel.Controls.Add(inputText);
			inputPanel.Controls.Add(browseInput);
			inputPanel.Controls.Add(inputLabel);

			// Format selection
			var formatPanel = new Panel { Dock = DockStyle.Top, Height = 60 };
			var formatLabel = new Label { Text = "Output Format:", Dock = DockStyle.Left, Width = 80 };
			formatBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
			formatBox.Enabled = false;

			formatPanel.Controls.Add(formatBox);
			formatPanel.Controls.Add(formatLabel);

			// Progress indicators
			progressBar = new ProgressBar { Dock = DockStyle.Top, Height = 20 };
			statusLabel = new Label { Text = "Ready", Dock = DockStyle.Top, Height = 30 };

			// Convert button
			convertBtn = new Button { Text = "Convert", Dock = DockStyle.Top, Height = 40, Enabled = false };
			convertBtn.Click += ConvertBtn_Click;

			// Add all controls to form
			Controls.Add(convertBtn);
			Controls.Add(statusLabel);
			Controls.Add(progressBar);
			Controls.Add(formatPanel);
			Controls.Add(inputPanel);
		}

		private async void BrowseInput_Click(object sender, EventArgs e)
		{
			using var dlg = new OpenFileDialog
			{
				Filter = "Media Files|*.mp3;*.mp4;*.wav;*.m4a;*.avi;*.mkv|All Files|*.*",
				Multiselect = false
			};

			if (dlg.ShowDialog() != DialogResult.OK) return;

			inputText.Text = dlg.FileName;
			formatBox.Items.Clear();
			formatBox.Enabled = false;
			convertBtn.Enabled = false;
			statusLabel.Text = "Analyzing file...";

			try
			{
				var mediaInfo = await FFProbe.AnalyseAsync(dlg.FileName);
				var hasAudio = mediaInfo.PrimaryAudioStream != null;
				var hasVideo = mediaInfo.PrimaryVideoStream != null;

				// Populate format options based on input capabilities
				foreach (var format in _outputFormats)
				{
					if ((hasAudio && format.Key.Contains("MP3")) ||
						(hasAudio && format.Key.Contains("WAV")) ||
						(hasVideo && format.Key.Contains("MP4")) ||
						(hasVideo && format.Key.Contains("AVI")))
					{
						formatBox.Items.Add(format.Key);
					}
				}

				if (formatBox.Items.Count > 0)
				{
					formatBox.SelectedIndex = 0;
					formatBox.Enabled = true;
					convertBtn.Enabled = true;
					statusLabel.Text = "Ready to convert";
				}
				else
				{
					statusLabel.Text = "No compatible output formats";
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error analyzing file:\n{ex.Message}", "Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				statusLabel.Text = "File analysis failed";
			}
		}

		private async void ConvertBtn_Click(object sender, EventArgs e)
		{
			var inputFile = inputText.Text;
			if (!File.Exists(inputFile))
			{
				MessageBox.Show("Input file does not exist", "Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (formatBox.SelectedItem == null)
			{
				MessageBox.Show("Please select an output format", "Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			var selectedFormat = formatBox.SelectedItem.ToString();
			if (!_outputFormats.TryGetValue(selectedFormat, out var formatConfig))
			{
				MessageBox.Show("Invalid format selected", "Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// Determine output extension
			string outputExtension = selectedFormat switch
			{
				string s when s.Contains("MP3") => ".mp3",
				string s when s.Contains("WAV") => ".wav",
				string s when s.Contains("MP4") => ".mp4",
				string s when s.Contains("AVI") => ".avi",
				_ => ".mp4"
			};

			var outputFile = Path.Combine(
				Path.GetDirectoryName(inputFile),
				$"{Path.GetFileNameWithoutExtension(inputFile)}_converted{outputExtension}");

			// Setup conversion
			convertBtn.Enabled = false;
			formatBox.Enabled = false;
			browseInput.Enabled = false;
			progressBar.Value = 0;
			statusLabel.Text = "Starting conversion...";

			try
			{
				// Create the progress callback
				Action<double> progressCallback = percent =>
				{
					this.Invoke(() =>
					{
						// Ensure the value stays between 0 and 100
						int progressValue = (int)(Math.Clamp(percent, 0.0, 1.0) * 100);
						progressBar.Value = progressValue;
						statusLabel.Text = $"Converting... {progressValue}%";
					});
				};

				await FFMpegArguments
					.FromFileInput(inputFile)
					.OutputToFile(outputFile, true, formatConfig)
					.NotifyOnProgress(progressCallback, TimeSpan.FromSeconds(1))
					.ProcessAsynchronously();

				statusLabel.Text = $"Conversion complete: {Path.GetFileName(outputFile)}";
				MessageBox.Show("Conversion completed successfully!", "Success",
					MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Conversion failed:\n{ex.Message}", "Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				statusLabel.Text = "Conversion failed";
			}
			finally
			{
				convertBtn.Enabled = true;
				formatBox.Enabled = true;
				browseInput.Enabled = true;
			}
		}
	}
}